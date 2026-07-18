using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private Transform enemyRoot;

    private GameObject currentEnemy;

    [SerializeField] private int playerRoomID;

    [SerializeField] private int enemyRoomID;

    AIPathfinder pathfinder;

    [SerializeField]
    private float moveInterval = 2f;

    private float timer;

    private AIState state = AIState.Patrol;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        enemyRoomID = 1;

        pathfinder = new AIPathfinder(Map_Manager.Instance.Graph);
    }


    public void SetEnemyRoom(int roomID)
    {
        enemyRoomID = roomID;

        CheckSameRoom();
    }
    private void CheckSameRoom()
    {
        if (enemyRoomID == playerRoomID)
        {
            state = AIState.Chase;
        }
        else
        {
            state = AIState.Patrol;
        }

        UpdateEnemyState();
    }

    public void PlayerEnteredRoom(int roomID)
    {
        playerRoomID = roomID;
        if (enemyRoomID == playerRoomID)
        {
            state = AIState.Chase;
        }

        UpdateEnemyState();
    }

    private void UpdateEnemyState()
    {
        if (playerRoomID == enemyRoomID)
        {
            SpawnEnemy();
        }
        else
        {
            RemoveEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Debug.Log(currentEnemy == null ? "NULL" : currentEnemy.name);
        if (currentEnemy != null)
            return;

        
        currentEnemy =
            Instantiate(enemyPrefab, enemyRoot);

        currentEnemy
            .GetComponent<EnemyController>()
            .Initialize();
    }

    private void RemoveEnemy()
    {
        Debug.Log("RemoveEnemy");
        if (currentEnemy == null)
            return;

        Destroy(currentEnemy);

        currentEnemy = null;
    }


    private void Update()
    {
        timer += Time.deltaTime;

        if (timer < moveInterval)
            return;

        timer = 0;

        Tick();
    }

    private void Tick()
    {
        switch (state)
        {
            case AIState.Patrol:
                Patrol();
                break;

            case AIState.Chase:
                Chase();
                break;
        }

        if (enemyRoomID != playerRoomID)
        {
            state = AIState.Patrol;
        }

        UpdateEnemyState();
    }

    private void Patrol()
    {
        List<int> neighbors =
            Map_Manager.Instance.Graph.GetNeighbors(enemyRoomID);

        if (neighbors.Count == 0)
            return;

        int previousRoom = enemyRoomID;

        SetEnemyRoom(neighbors[Random.Range(0, neighbors.Count)]);

        Debug.Log($"AI ¿Ãµø : {previousRoom} °Ê {enemyRoomID}");
    }

    private void Chase()
    {
        List<int> path =
            pathfinder.FindPath(enemyRoomID, playerRoomID);

        if (path.Count > 1)
        {
            SetEnemyRoom(path[1]);
        }
    }

    
}