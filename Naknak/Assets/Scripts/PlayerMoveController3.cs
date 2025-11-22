using UnityEngine;

// enum Direction 은 PlayerMoveController 의 enum 을 따릅니다.

public class PlayerMoveController3 : MonoBehaviour
{
    public float moveDuration = 0.3f;
    private readonly float moveDistance = 1f;
    private readonly float sameInputTime = 0.85f;
    Animator anim;
    LastInputManager lastInputManager;

    // 추상 현재 위치
    Vector2 currentPosition;

    // 플레이어 애니메이션 우선 순위를 위함
    bool xFirst = false, yFirst = false;

    private bool _isMoving;
    private bool isMoving
    {
        get => _isMoving;
        set
        {
            anim.SetBool("isMoving", value);
            _isMoving = value;
        }
    } // backing field로 불필요한 GetBool 메소드 사용 수정


    // 플레이어 충돌 판정을 위해 일단 추가
    public int floor = 1;
    int layer
    {
        get
        {
            if (floor == 1) return LayerMask.GetMask("Col 1F");
            else if (floor == 2) return LayerMask.GetMask("Col 2F");
            else if (floor == 3) return LayerMask.GetMask("Col 3F");
            else return LayerMask.GetMask("Col 1F");
        }
        set { return; }
    }

    // --- X축 이동 변수 ---
    private Vector2 startPosX;
    private Vector2 targetPosX;
    private bool[] canMoveXY = {true, true}; // 0번은 top, 1번은 bottom
    private Vector2 currentDirectionX;
    private Vector2 _queuedDirectionX;
    private Vector2 queuedDirectionX
    {
        get { return _queuedDirectionX; }
        set
        {
            if (value != currentDirectionX || nextInputX) _queuedDirectionX = value;
        }
    }
    private bool nextInputX = false;
    internal bool isMovingX = false;
    private float elapsedTimeX = 0f;

    // --- Y축 이동 변수 ---
    private Vector2 startPosY;
    private Vector2 targetPosY;
    private bool[] canMoveYX = {true, true}; // 0번은 left, 1번은 right
    private Vector2 currentDirectionY;
    private Vector2 _queuedDirectionY;
    private Vector2 queuedDirectionY
    {
        get { return _queuedDirectionY; }
        set
        {
            if (value != currentDirectionY || nextInputY) _queuedDirectionY = value;
        }
    }
    private bool nextInputY = false;
    internal bool isMovingY = false;
    private float elapsedTimeY = 0f;

    Vector3 gridPreset;

    void Start()
    {
        anim = GetComponent<Animator>();
        lastInputManager = GetComponent<LastInputManager>();
        
        currentPosition = transform.position;

        // X, Y축 변수 초기화
        startPosX = transform.position;
        targetPosX = transform.position;
        startPosY = transform.position;
        targetPosY = transform.position;

        gridPreset = MapManager.Instance.World2Grid(Vector3.zero);
    }

    void Update()
    {
        if (GameStateManager.Instance.GameState == GameState.Gameplay)
        {
            UpdateMoveX();
            UpdateMoveY();
        }

        isMoving = isMovingX || isMovingY;

        // 플레이어 애니메이션
        // X, Y 완전히 동시에 눌리면 X 우선순위
        if (xFirst && !isMovingX) xFirst = false;
        if (yFirst && !isMovingY) yFirst = false;

        if (!yFirst && isMovingX) xFirst = true;
        else if (!xFirst && isMovingY) yFirst = true;

        if (xFirst && isMovingX) anim.SetFloat("direction", (float)vector2Dir(currentDirectionX));
        else if (yFirst && isMovingY) anim.SetFloat("direction", (float)vector2Dir(currentDirectionY));

        if (lastInputManager.GetKeyDownInteract() && !isMoving) TryInteract();

    }

    void TryInteract()
    {   
        GameState gameState = GameStateManager.Instance.GameState;
        if (gameState == GameState.Dialog)
        {
            GameEventBase evt = GameEventFactory.CreateNextDialogEvent();
            GameEventManager.Instance.Submit(evt);
        } 
        else if (gameState == GameState.Gameplay)
        {
            // 현재 바라보는 방향을 기록하는 변수가 없어서 우선 dir를 right로 설정했어요.
            // dir = currentDirectionX + currentDirectionY;
            // 하려고 시도 했는데, 논리상으로 틀린 식이더라구요. 새로운 변수를 만들어야 할 것 같습니다.
            Vector2 dir = Vector2.right;
            Vector3 from = transform.position;
            
            RaycastHit2D hit = Physics2D.Raycast(from - new Vector3(0f, 0.5f, 0f), dir, moveDistance, layer);
            Debug.DrawRay(from - new Vector3(0f, 0.5f, 0f), dir * moveDistance, Color.red);

            if (hit.collider != null && hit.collider.CompareTag("Interactable"))
            {
                Debug.Log("[PlayerMoveController3] Interact : " + hit.collider.name);
                hit.collider.GetComponent<IInteractable>().Interact();
            }
        }
    }

    // --- X축 로직 ---
    void UpdateMoveX()
    {
        EnqueueMoveX();

        if (!isMovingX && queuedDirectionX != Vector2.zero)
        {
            StartMoveX();
        }

        if (isMovingX)
        {
            elapsedTimeX += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTimeX / moveDuration);

            Vector2 newPos = new Vector2(Mathf.Lerp(startPosX.x, targetPosX.x, t), transform.position.y);
            transform.position = newPos;

            if (t >= sameInputTime) nextInputX = true;

            bool arrived = false;
            if (Mathf.Abs(transform.position.x - targetPosX.x) <= 0.001f)
            {
                transform.position = new Vector2(targetPosX.x, transform.position.y);
                arrived = true;
            }

            if (arrived)
            {
                startPosX = targetPosX;

                syncCanMoveXY(targetPosX);
                syncCanMoveYX(targetPosX);

                if (queuedDirectionX != Vector2.zero &&                                     // (1-1) 입력 큐에 원소가 존재한다면
                    (lastInputManager.GetLastInputAxis() == "Horizontal" ||                 // (2-1) 마지막 입력 방향이 수평 방향이거나
                    (lastInputManager.GetAxisRaw("Vertical") == 1f && !canMoveXY[0])||      // (2-2) 마지막 입력이 수직, 위 방향인데 위로 움직일 수 없거나
                    (lastInputManager.GetAxisRaw("Vertical") == -1f && !canMoveXY[1])))     // (2-3) 마지막 입력이 수직, 아래 방향인데 아래로 움직일 수 없다면
                    { StartMoveX(); }
                else {
                    isMovingX = false;
                }
            }
        }
    }

    private void EnqueueMoveX()
    {
        float moveValue = lastInputManager.GetAxisRaw("Horizontal");
        if (Mathf.Abs(moveValue) == 1f)
        {
            queuedDirectionX = new Vector2(moveValue, 0f);
        }
    }

    private void StartMoveX()
    {
        currentDirectionX = queuedDirectionX;
        startPosX = currentPosition;

        syncCanMoveXY(currentPosition);

        bool diagonalBlocked = queuedDirectionX == Vector2.left && !canMoveYX[0] ||     // (1-1) 입력 큐의 방향이 왼쪽인데 왼쪽으로 움직일 수 없거나 
                               queuedDirectionX == Vector2.right && !canMoveYX[1];      // (1-2) 입력 큐의 방향이 오른쪽인데 오른쪽으로 움직일 수 없다면
                                                                                        // -> 대각선 이동 불가 상황

        if (checkCollider(currentPosition, currentDirectionX) || diagonalBlocked) {
            if (!isMoving) anim.SetFloat("direction", (float)vector2Dir(currentDirectionX));
            isMovingX = false;
            nextInputX = true;
            elapsedTimeX = 0f;
            queuedDirectionX = Vector2.zero;
        }
        else
        {
            targetPosX = startPosX + currentDirectionX * moveDistance;
            currentPosition.x = targetPosX.x;

            isMovingX = true;
            nextInputX = false;
            elapsedTimeX = 0f;
            queuedDirectionX = Vector2.zero;
        }
    }

    // --- Y축 로직 ---
    void UpdateMoveY()
    {
        EnqueueMoveY();

        if (!isMovingY && queuedDirectionY != Vector2.zero)
        {
            StartMoveY();
        }

        if (isMovingY)
        {
            elapsedTimeY += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTimeY / moveDuration);

            Vector2 newPos = new Vector2(transform.position.x, Mathf.Lerp(startPosY.y, targetPosY.y, t));
            transform.position = newPos;

            if (t >= sameInputTime) nextInputY = true;

            bool arrived = false;
            if (Mathf.Abs(transform.position.y - targetPosY.y) <= 0.001f)
            {
                transform.position = new Vector2(transform.position.x, targetPosY.y);
                arrived = true;
            }

            if (arrived)
            {
                startPosY = targetPosY;

                syncCanMoveXY(targetPosY);
                syncCanMoveYX(targetPosY);

                if (queuedDirectionY != Vector2.zero &&                                     // (1-1) 입력 큐에 원소가 존재한다면
                    (lastInputManager.GetLastInputAxis() == "Vertical" ||                   // (2-1) 마지막 입력이 수직 방향이거나          
                    (lastInputManager.GetAxisRaw("Horizontal") == -1f && !canMoveYX[0]) ||  // (2-2) 마지막 입력이 수평, 왼쪽 방향인데 왼쪽으로 움직일 수 없거나
                    (lastInputManager.GetAxisRaw("Horizontal") == 1f && !canMoveYX[1])))    // (2-3) 마지막 입력이 수평, 오른쪽 방향인데 오른쪽으로 움직일 수 없다면
                    { StartMoveY(); }                                                 
                else
                {
                    isMovingY = false;
                }
            }
        }
    }

    private void EnqueueMoveY()
    {
        float moveValue = lastInputManager.GetAxisRaw("Vertical");
        if (Mathf.Abs(moveValue) == 1f)
        {
            queuedDirectionY = new Vector2(0f, moveValue);
        }
    }

    private void StartMoveY()
    {
        currentDirectionY = queuedDirectionY;
        startPosY = currentPosition;

        syncCanMoveYX(currentPosition);

        bool diagonalBlocked = queuedDirectionY == Vector2.up && !canMoveXY[0] ||       // (1-1) 입력 큐의 방향이 위쪽인데 위쪽으로 움직일 수 없거나
                               queuedDirectionY == Vector2.down && !canMoveXY[1];       // (1-2) 입력 큐의 방향이 아래쪽인데 아래쪽으로 움직일 수 없다면
                                                                                        // -> 대각선 이동 불가 상황

        if (checkCollider(currentPosition, currentDirectionY) || diagonalBlocked) {
            if (!isMoving) anim.SetFloat("direction", (float)vector2Dir(currentDirectionY));
            isMovingY = false;
            nextInputY = true;
            elapsedTimeY = 0f;
            queuedDirectionY = Vector2.zero;
        }
        else
        {
            targetPosY = startPosY + currentDirectionY * moveDistance;
            currentPosition.y = targetPosY.y;

            isMovingY = true;
            nextInputY = false;
            elapsedTimeY = 0f;
            queuedDirectionY = Vector2.zero;
        }
    }

    /// <summary>
    /// NPC의 위치를 파악하기 위해 이미 RayCast가 필요한 상황이라서
    /// 아마 다시 RayCast로 콜라이더를 감지하는 것으로 수정할 것 같습니다.
    /// </summary>
    bool checkCollider(Vector3 from, Vector3 dir)
    {
        bool check = false;
        
        RaycastHit2D hit = Physics2D.Raycast(from - new Vector3(0f, 0.5f, 0f), dir, moveDistance, layer);
        Debug.DrawRay(from - new Vector3(0f, 0.5f, 0f), dir * moveDistance, Color.red);

        // Delayed Evaluation
        check = MapManager.Instance.IsCollision(from - new Vector3(0f, 0.5f, 0f) + dir * moveDistance - gridPreset)
            || (hit.collider != null && hit.collider.CompareTag("Interactable"));

        return check;
    }

    // --- 공용 메소드 ---
    Direction vector2Dir(Vector2 vec)
    {
        if (vec.y == 1f) return Direction.Up;
        if (vec.x == 1f) return Direction.Right;
        if (vec.y == -1f) return Direction.Down;
        if (vec.x == -1f) return Direction.Left;
        return Direction.Down;
    }

    void syncCanMoveXY(Vector2 pos){
        canMoveXY[0] = !checkCollider(pos, Vector2.up);
        canMoveXY[1] = !checkCollider(pos, Vector2.down);
    }

    void syncCanMoveYX(Vector2 pos){
        canMoveYX[0] = !checkCollider(pos, Vector2.left);
        canMoveYX[1] = !checkCollider(pos, Vector2.right);
    }
}