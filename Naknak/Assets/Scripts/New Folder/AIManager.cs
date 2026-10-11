using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private Transform player;

    // [SerializeField] Closet closet;

    [SerializeField] private float moveInterval = 2f;
    [SerializeField] private float blindTime = 7f;
    [SerializeField] private float teleportDelayAfterDebuff = 3f;
    [SerializeField, Range(0f, 1f)] private float fireTeleportChance = 0.5f;
    [SerializeField] private int spawnRingDistance = 3;
    [SerializeField] private int patrolStartBurningRoomId = 1;
    [SerializeField] private int[] patrolRoute = { 2, 3, 4, 5, 6, 7, 8, 11 };

    private GameObject currentEnemy;
    [SerializeField] private int playerRoomID;
    [SerializeField] private int enemyRoomID;

    private AIPathfinder pathfinder;
    private float timer;
    private AIState state = AIState.Patrol;
    [SerializeField] private float debuff = 0f;

    private bool aiStarted;
    private bool chaseActive;
    private float chaseTimer;
    private Coroutine teleportCoroutine;
    private int lastTriggeredBurningRoomId;

    public bool isEnemyEvent= false;
    private int eventCount = 2;

    public int ReturnPlayerRoomID() => playerRoomID;

    private void Awake()
    {
        Instance = this;
        state = AIState.Patrol;
    }

    private void Start()
    {
        enemyRoomID = 2;
        playerRoomID = 11;
        pathfinder = new AIPathfinder();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        RemoveEnemy();
    }

    public void SetEnemyRoom(int roomID)
    {
        enemyRoomID = roomID;
        UpdateEnemyState();
    }

    public void PlayerEnteredRoom(int roomID)
    {
        playerRoomID = roomID;
        UpdateEnemyState();
    }

    private void UpdateEnemyState()
    {
        if (isEnemyEvent)
        {
            state = AIState.Event;
            return;
        }


        if (currentEnemy == null)
        {
            state = AIState.Patrol;
            return;
        }

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy == null)
            return;

        if (enemyRoomID == playerRoomID)
        {
            state = AIState.GridChase;
            enemy.SetGridChase(true);
            return;
        }

        enemy.SetGridChase(false);
        state = chaseActive ? AIState.RoomChase : AIState.Patrol;
    }

    public void SpawnEnemyAtRoom(int roomId)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab missing");
            return;
        }

        if (currentEnemy == null)
        {
            currentEnemy = enemyRoot != null
                ? Instantiate(enemyPrefab, enemyRoot)
                : Instantiate(enemyPrefab);
        }

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy != null)
            enemy.Initialize();

        enemyRoomID = roomId;

        if (MansionRoomSystem.Instance != null)
            MansionRoomSystem.Instance.MoveEnemyToRoom(currentEnemy, enemyRoomID);

        UpdateEnemyState();
    }

    private void RemoveEnemy()
    {
        if (currentEnemy == null)
            return;

        Destroy(currentEnemy);
        currentEnemy = null;
        state = AIState.Patrol;
    }

    private void Update()
    {
        int burningRoomId = MansionRoomSystem.Instance != null
            ? MansionRoomSystem.Instance.GetBurningRoomId()
            : 0;

        TryStartAI(burningRoomId);
        HandleBurningRoomDebuff(burningRoomId);
        HandleChaseTimeout();

        if (!aiStarted || currentEnemy == null)
            return;

        timer += Time.deltaTime;
        if (timer < moveInterval)
            return;

        timer = 0f;
        Tick();
    }

    private void TryStartAI(int burningRoomId)
    {
        if (aiStarted)
            return;

        if (burningRoomId != patrolStartBurningRoomId)
            return;

        aiStarted = true;
        chaseActive = false;
        debuff = 0f;
        enemyRoomID = GetFirstPatrolRoom();
        SpawnEnemyAtRoom(enemyRoomID);

        Debug.Log($"AI patrol started after Room {patrolStartBurningRoomId} burning.");
    }

    private void HandleBurningRoomDebuff(int burningRoomId)
    {
        bool playerInBurningRoom = burningRoomId != 0 && playerRoomID == burningRoomId;

        if (!playerInBurningRoom)
        {
            if (lastTriggeredBurningRoomId == burningRoomId)
                lastTriggeredBurningRoomId = 0;

            return;
        }

        if (!aiStarted)
            return;

        if (lastTriggeredBurningRoomId == burningRoomId && chaseActive)
            return;

        lastTriggeredBurningRoomId = burningRoomId;
        StartChaseDebuff();
    }

    private void StartChaseDebuff()
    {
        chaseActive = true;
        chaseTimer = blindTime;
        debuff = blindTime;

        if (AlterSpawnManager.Instance != null)
            AlterSpawnManager.Instance.TriggerBlindDebuff(blindTime);

        if (teleportCoroutine != null)
            StopCoroutine(teleportCoroutine);

        teleportCoroutine = StartCoroutine(TryTeleportAfterDelay());
        UpdateEnemyState();
    }

    private IEnumerator TryTeleportAfterDelay()
    {
        yield return new WaitForSeconds(teleportDelayAfterDebuff);

        teleportCoroutine = null;

        if (!chaseActive || currentEnemy == null)
            yield break;

        if (Random.value <= fireTeleportChance)
        {
            TeleportEnemyNearPlayer();
            Debug.Log("Enemy fire chase teleport success");
        }
        else
        {
            Debug.Log("Enemy fire chase teleport skipped");
        }

        UpdateEnemyState();
    }

    private void HandleChaseTimeout()
    {
        if (!chaseActive)
            return;

        chaseTimer -= Time.deltaTime;
        debuff = Mathf.Max(0f, chaseTimer);

        if (chaseTimer > 0f)
            return;

        if (enemyRoomID == playerRoomID)
        {
            UpdateEnemyState();
            return;
        }

        chaseActive = false;
        debuff = 0f;

        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
            teleportCoroutine = null;
        }

        UpdatePatrolTargetFromCurrentRoom();
        UpdateEnemyState();
    }

    private void TeleportEnemyNearPlayer()
    {
        if (currentEnemy == null)
            return;

        currentEnemy.transform.position = GetEnemySpawnPosition();
        enemyRoomID = playerRoomID;
    }

    private Vector3 GetEnemySpawnPosition()
    {
        if (player == null)
            return currentEnemy != null ? currentEnemy.transform.position : Vector3.zero;

        GridLayout grid = TriggerExecutor.Instance != null ? TriggerExecutor.Instance.grid : null;
        if (grid == null)
            return player.position;

        Vector3Int playerCell = grid.WorldToCell(player.position);
        List<Vector3> candidates = new List<Vector3>();

        for (int x = -spawnRingDistance; x <= spawnRingDistance; x++)
        {
            for (int y = -spawnRingDistance; y <= spawnRingDistance; y++)
            {
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != spawnRingDistance)
                    continue;

                Vector3Int cell = new Vector3Int(playerCell.x + x, playerCell.y + y, playerCell.z);
                Vector3 worldPos = grid.CellToWorld(cell) + grid.cellSize * 0.5f;
                worldPos.z = player.position.z;

                if (!HasTilemapCollider(worldPos))
                    candidates.Add(worldPos);
            }
        }

        if (candidates.Count == 0)
            return player.position;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool HasTilemapCollider(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.GetComponent<TilemapCollider2D>() != null)
                return true;
        }

        return false;
    }

    private void Tick()
    {


        switch (state)
        {
            case AIState.Patrol:
                Patrol();
                break;
            case AIState.RoomChase:
                RoomChase();
                break;
            case AIState.GridChase:
                GridChase();
                break;
        }

        UpdateEnemyState();
    }

    private int patrolTargetIndex;

    private int GetFirstPatrolRoom()
    {
        if (patrolRoute == null || patrolRoute.Length == 0)
            return 2;

        patrolTargetIndex = 0;
        return patrolRoute[0];
    }

    private int GetCurrentPatrolTarget()
    {
        if (patrolRoute == null || patrolRoute.Length == 0)
            return enemyRoomID;

        if (patrolTargetIndex < 0 || patrolTargetIndex >= patrolRoute.Length)
            patrolTargetIndex = 0;

        return patrolRoute[patrolTargetIndex];
    }

    private void AdvancePatrolTarget()
    {
        if (patrolRoute == null || patrolRoute.Length == 0)
            return;

        patrolTargetIndex = (patrolTargetIndex + 1) % patrolRoute.Length;
    }

    private void UpdatePatrolTargetFromCurrentRoom()
    {
        if (patrolRoute == null || patrolRoute.Length == 0)
            return;

        for (int i = 0; i < patrolRoute.Length; i++)
        {
            if (patrolRoute[i] == enemyRoomID)
            {
                patrolTargetIndex = (i + 1) % patrolRoute.Length;
                return;
            }
        }

        patrolTargetIndex = 0;
    }

    private void Patrol()
    {
        if (currentEnemy == null || MansionRoomSystem.Instance == null)
            return;

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy == null || enemy.IsRoomMoving)
            return;

        int targetRoom = GetCurrentPatrolTarget();
        if (enemyRoomID == targetRoom)
        {
            AdvancePatrolTarget();
            targetRoom = GetCurrentPatrolTarget();
        }

        MoveOneRoomToward(targetRoom, "AI Patrol");
    }

    private void RoomChase()
    {
        if (currentEnemy == null || MansionRoomSystem.Instance == null)
            return;

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy == null || enemy.IsRoomMoving)
            return;

        MoveOneRoomToward(playerRoomID, "AI RoomChase");
    }

    private void MoveOneRoomToward(int targetRoom, string logPrefix)
    {
        if (pathfinder == null)
            pathfinder = new AIPathfinder();

        List<int> path = pathfinder.FindPath(enemyRoomID, targetRoom);
        if (path.Count <= 1)
        {
            Debug.Log($"{logPrefix} path missing: {enemyRoomID} -> {targetRoom}");
            return;
        }

        int previousRoom = enemyRoomID;
        int nextRoom = path[1];

        RoomMoveInfo info = MansionRoomSystem.Instance.GetRoomMoveInfo(enemyRoomID, nextRoom);
        if (info == null)
            return;

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy == null)
            return;

        enemy.MoveToRoom(info);
        Debug.Log($"{logPrefix}: {previousRoom} -> {nextRoom} / Target {targetRoom}");
    }

    private void GridChase()
    {
        if (currentEnemy == null)
            return;

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy == null)
            return;

        enemy.SetGridChase(true);
    }

    public void EnemyEvent(int spawnRoomID)
    {
        ++eventCount;
        isEnemyEvent = true;

        if (eventCount == 1)
            StartCoroutine(EnemyEvent01(spawnRoomID));
        else if (eventCount == 2)
            StartCoroutine(EnemyEvent02(spawnRoomID));
        else if (eventCount == 3)
            StartCoroutine(EnemyEvent03(spawnRoomID));
    }

    IEnumerator EnemyEvent01(int spawnRoomID)
    {
        SpawnEnemyAtRoom(spawnRoomID);

        yield return new WaitForSeconds(3f);

        RemoveEnemy();
        isEnemyEvent = false;

        Test1 test = GetComponent<Test1>();
        test.check = false;
    }

    IEnumerator EnemyEvent02(int spawnRoomID)
    {
        
        SpawnEnemyAtRoom(spawnRoomID);

        GameObject obj = GameObject.Find("Enemy(Clone)");
        EnemyEventCollider enemyEventCollider = obj.GetComponent<EnemyEventCollider>();

        while (!enemyEventCollider.IsPlayerInRange())
        {
            yield return null;
        }

        RemoveEnemy();
        isEnemyEvent = false;

        Test1 test = GetComponent<Test1>();
        test.check = false;

    }

    IEnumerator EnemyEvent03(int spawnRoomID)
    {

        SpawnEnemyAtRoom(spawnRoomID);

        GameObject obj = GameObject.Find("Enemy(Clone)");
        EnemyEventCollider enemyEventCollider = obj.GetComponent<EnemyEventCollider>();

        while (!enemyEventCollider.IsPlayerInRange())
        {
            yield return null;
        }

        Debug.Log("추적 상태 전환");
        isEnemyEvent = false;
        state = AIState.GridChase;
        UpdateEnemyState();

        Test1 test = GetComponent<Test1>();
        test.check = false;


    }
    public void EnemyArrived(int roomID)
    {
        enemyRoomID = roomID;
        Debug.Log($"EnemyRoomID changed: {enemyRoomID}");
        UpdateEnemyState();
    }

    public void RoomDeleted(int roomID)
    {
        if (MansionRoomSystem.Instance == null)
            return;

        if (enemyRoomID != roomID)
        {
            UpdatePatrolTargetFromCurrentRoom();
            UpdateEnemyState();
            return;
        }

        if (currentEnemy == null)
        {
            enemyRoomID = MansionRoomSystem.Instance.GetSafeAIRoomAfterDelete(roomID);
            UpdatePatrolTargetFromCurrentRoom();
            UpdateEnemyState();
            return;
        }

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
        if (enemy != null)
            enemy.CancelRoomMove();

        int safeRoomId = MansionRoomSystem.Instance.GetSafeAIRoomAfterDelete(roomID);
        if (safeRoomId == 13)
        {
            RemoveEnemy();
            return;
        }

        enemyRoomID = safeRoomId;
        MansionRoomSystem.Instance.MoveEnemyToRoom(currentEnemy, enemyRoomID);
        UpdatePatrolTargetFromCurrentRoom();
        UpdateEnemyState();
    }

    public void EnemyRoomMoveFailed(int failedTargetRoomId)
    {
        Debug.LogWarning($"Enemy room move failed. Target room: {failedTargetRoomId}");

        if (state == AIState.Patrol && GetCurrentPatrolTarget() == failedTargetRoomId)
            AdvancePatrolTarget();

        UpdateEnemyState();
    }
}

