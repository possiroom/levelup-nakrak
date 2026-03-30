using System;
using System.Collections;
using UnityEngine;

// enum Direction 은 PlayerMoveController 의 enum 을 따릅니다.

public class PlayerMoveController3 : MonoBehaviour
{
    public float moveDuration = 0.3f;
    public float runDuration = 0.2f;
    private readonly float moveDistance = 1f;
    private readonly float sameInputTime = 0.85f;

    public LayerMask floorLayer;
    
    Animator anim;
    LastInputManager lastInputManager;

    public BoxCollider2D col;

    // 추상 현재 위치
    Vector2 currentPosition;

    // 플레이어가 바라보는 방향 (항상 유지됨)
    private Vector2 facingDirection = Vector2.down;

    // 플레이어 애니메이션 우선 순위를 위함
    bool xFirst = false, yFirst = false;
    bool teleporting = false;

    private bool _isMoving;
    public bool isMoving
    {
        get => _isMoving;
        private set
        {
            anim.SetBool("isMoving", value);
            _isMoving = value;
        }
    } // backing field로 불필요한 GetBool 메소드 사용 수정


    //플레이어 층수
    int _layer = 1;
    int layer
    {
        get
        {
            if (_layer == 1) return LayerMask.GetMask("Col 1F");
            else if (_layer == 2) return LayerMask.GetMask("Col 2F");
            else if (_layer == 3) return LayerMask.GetMask("Col 3F");
            else return LayerMask.GetMask("Col 1F");
        }
        set
        {
            _layer = value;
        }
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

    // --- 점프 관련 변수 ---
    private Vector2 jumpStartPos;
    private int queuedJump = 0;
    private bool isJumping = false;
    private bool jumpPhase1 = false;
    private bool jumpPhase2 = false;
    private float jumpDuration = 0.6f;
    private float elapsedTimeJump = 0f;

    private float jumpHeight = 1.5f;
    private int rho = 60;



    void Start()
    {
        anim = GetComponent<Animator>();
        lastInputManager = GetComponent<LastInputManager>();
        col = GetComponent<BoxCollider2D>();
        
        currentPosition = transform.position;

        // X, Y축 변수 초기화
        startPosX = transform.position;
        targetPosX = transform.position;
        startPosY = transform.position;
        targetPosY = transform.position;
    }

    void Update()
    {
        if (GameStateManager.Instance.GameState == GameState.Gameplay)
        {
            if (!isJumping)
            {
                UpdateMoveX();
                UpdateMoveY();
            }
            UpdateJump();
            if (isMovingX || isMovingY) anim.speed = lastInputManager.GetKeyRun() ? moveDuration/runDuration : 1f;
            else anim.speed = 1f;
        } 
        else
        {
            // dequeue Move XY
            queuedDirectionX = Vector2.zero;
            queuedDirectionY = Vector2.zero;
            anim.speed = 0f; // ...
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
        else if (!isMoving) anim.SetFloat("direction", (float)vector2Dir(facingDirection));
        
        if (lastInputManager.GetKeyDownInteract() && 
        (!isMoving || GameStateManager.Instance.GameState == GameState.Dialog)) TryInteract();   
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
            // 현재  facingDir 함수 사용해서 기록하고 있습니다.
            Vector2 dir = facingDirection;
            Vector3 from = transform.position;
            
            RaycastHit2D[] hits = Physics2D.RaycastAll(from - new Vector3(0f, 0.5f, 0f), dir, moveDistance, layer);
            Debug.DrawRay(from - new Vector3(0f, 0.5f, 0f), dir * moveDistance, Color.red);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Interactable"))
                {
                    Debug.Log("[PlayerMoveController3] Interact : " + hit.collider.name);
                    hit.collider.GetComponent<IInteractable>().Interact();
                    break;
                }
            }
        }
    }

    // facingDir function removed. `facingDirection` is updated in movement methods.

    // --- X축 로직 ---
    void UpdateMoveX()
    {
        EnqueueMoveX();
        if (teleporting) queuedDirectionX = Vector2.zero;

        if (!isMovingX && queuedDirectionX != Vector2.zero)
        {
            StartMoveX();
        }

        if (isMovingX)
        {
            facingDirection = currentDirectionX;
            elapsedTimeX += Time.deltaTime * (lastInputManager.GetKeyRun() ? moveDuration/runDuration : 1f);
            float t = Mathf.Clamp01(elapsedTimeX / moveDuration);

            Vector2 newPos = new Vector2(Mathf.Lerp(startPosX.x, targetPosX.x, t), transform.position.y);
            transform.position = newPos;

            if (t >= sameInputTime) nextInputX = true;

            bool arrived = false;
            if (t >= 1f)
            {
                transform.position = new Vector2(targetPosX.x, transform.position.y);
                arrived = true;
            }

            if (arrived)
            {
                startPosX = targetPosX;
                elapsedTimeX = 0f;
                bool pause = false;
                pause = TriggerExecutor.Instance.OnStepCompleted(targetPosX - new Vector2(0f, 0.5f));
                
                syncCanMoveXY(targetPosX);
                syncCanMoveYX(targetPosX);

                if (queuedDirectionX != Vector2.zero &&                                     // (1-1) 입력 큐에 원소가 존재한다면
                    (lastInputManager.GetLastInputAxis() == "Horizontal" ||                 // (2-1) 마지막 입력 방향이 수평 방향이거나
                    (lastInputManager.GetAxisRaw("Vertical") == 1f && !canMoveXY[0])||      // (2-2) 마지막 입력이 수직, 위 방향인데 위로 움직일 수 없거나
                    (lastInputManager.GetAxisRaw("Vertical") == -1f && !canMoveXY[1]))      // (2-3) 마지막 입력이 수직, 아래 방향인데 아래로 움직일 수 없다면
                    && !pause && queuedJump == 0)
                    { StartMoveX(); }
                else {
                    isMovingX = false;
                }
            }
        }
    }

    private void EnqueueMoveX()
    {
        if (isJumping || queuedJump != 0) return;
        float moveValue = lastInputManager.GetAxisRaw("Horizontal");
        if (Mathf.Abs(moveValue) == 1f)
        {
            queuedDirectionX = new Vector2(moveValue, 0f);
        }
    }

    private void StartMoveX()
    {
        currentDirectionX = queuedDirectionX;
        facingDirection = currentDirectionX;
        startPosX = currentPosition;

        syncCanMoveXY(currentPosition);

        bool diagonalBlocked = queuedDirectionX == Vector2.left && !canMoveYX[0] ||     // (1-1) 입력 큐의 방향이 왼쪽인데 왼쪽으로 움직일 수 없거나 
                               queuedDirectionX == Vector2.right && !canMoveYX[1];      // (1-2) 입력 큐의 방향이 오른쪽인데 오른쪽으로 움직일 수 없다면
                                                                                        // -> 대각선 이동 불가 상황

        if (checkCollider(currentPosition, currentDirectionX) || diagonalBlocked && isMovingY) {
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

            if (!isMovingY) anim.SetFloat("direction", (float)vector2Dir(currentDirectionX));
            TriggerExecutor.Instance.OnStepStarted(targetPosX - new Vector2(0f, 0.5f));
        }
    }

    // --- Y축 로직 ---
    void UpdateMoveY()
    {
        EnqueueMoveY();
        if (teleporting) queuedDirectionY = Vector2.zero;

        if (!isMovingY && queuedDirectionY != Vector2.zero)
        {
            StartMoveY();
        }

        if (isMovingY)
        {
            facingDirection = currentDirectionY;
            elapsedTimeY += Time.deltaTime * (lastInputManager.GetKeyRun() ? moveDuration/runDuration : 1f);
            float t = Mathf.Clamp01(elapsedTimeY / moveDuration);

            Vector2 newPos = new Vector2(transform.position.x, Mathf.Lerp(startPosY.y, targetPosY.y, t));
            transform.position = newPos;

            if (t >= sameInputTime) nextInputY = true;

            bool arrived = false;
            if (t >= 1f)
            {
                transform.position = new Vector2(transform.position.x, targetPosY.y);
                arrived = true;
            }

            if (arrived)
            {
                startPosY = targetPosY;
                elapsedTimeY = 0f;
                bool pause = false;
                pause = TriggerExecutor.Instance.OnStepCompleted(targetPosY - new Vector2(0f, 0.5f));

                syncCanMoveXY(targetPosY);
                syncCanMoveYX(targetPosY);

                if (queuedDirectionY != Vector2.zero &&                                     // (1-1) 입력 큐에 원소가 존재한다면
                    (lastInputManager.GetLastInputAxis() == "Vertical" ||                   // (2-1) 마지막 입력이 수직 방향이거나          
                    (lastInputManager.GetAxisRaw("Horizontal") == -1f && !canMoveYX[0]) ||  // (2-2) 마지막 입력이 수평, 왼쪽 방향인데 왼쪽으로 움직일 수 없거나
                    (lastInputManager.GetAxisRaw("Horizontal") == 1f && !canMoveYX[1]))     // (2-3) 마지막 입력이 수평, 오른쪽 방향인데 오른쪽으로 움직일 수 없다면
                    && !pause )
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
        if (isJumping || queuedJump != 0) return;
        float moveValue = lastInputManager.GetAxisRaw("Vertical");
        if (Mathf.Abs(moveValue) == 1f)
        {
            queuedDirectionY = new Vector2(0f, moveValue);
        }
    }

    private void StartMoveY()
    {
        currentDirectionY = queuedDirectionY;
        facingDirection = currentDirectionY;
        startPosY = currentPosition;

        syncCanMoveYX(currentPosition);

        bool diagonalBlocked = queuedDirectionY == Vector2.up && !canMoveXY[0] ||       // (1-1) 입력 큐의 방향이 위쪽인데 위쪽으로 움직일 수 없거나
                               queuedDirectionY == Vector2.down && !canMoveXY[1];       // (1-2) 입력 큐의 방향이 아래쪽인데 아래쪽으로 움직일 수 없다면
                                                                                        // -> 대각선 이동 불가 상황

        if (checkCollider(currentPosition, currentDirectionY) || diagonalBlocked && isMovingX) {
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

            if (!isMovingX) anim.SetFloat("direction", (float)vector2Dir(currentDirectionY));
            TriggerExecutor.Instance.OnStepStarted(targetPosY - new Vector2(0f, 0.5f));
        }
    }

    /// <summary>
    /// 위/아래로만 점프가 가능하고
    /// -> 보고 있는 방향이 위/아래일 때 StartJump 가능
    /// 우선은 멈춰있을 때에만 점프 할 수 있도록
    /// </summary>
    private void UpdateJump()
    {
        EnqueueJump();

        if (!isMovingX && queuedJump != 0)
        {
            StartJump();
        }

        if (isJumping)
        {
            elapsedTimeJump += Time.deltaTime;
            float tj = Mathf.Clamp01(elapsedTimeJump / jumpDuration);
            float ty = Mathf.Clamp01(elapsedTimeY / moveDuration);
            if (facingDirection == Vector2.up)
                transform.position = jumpStartPos + Vector2.up * GetJumpPos(tj);
            else if (facingDirection == Vector2.down)
                transform.position = jumpStartPos + Vector2.up * (GetJumpPos(1-tj) - 2f);

            if (ty + 2*tj >= 1f && !jumpPhase1) {
                jumpPhase1 = true;
                Debug.Log("[PlayerMoveController3] Jump Phase 1");

                currentPosition = (jumpStartPos + targetPosY) / 2f - ty * facingDirection;
                Debug.Log("Currnent Position: " + currentPosition.ToString());
                TriggerExecutor.Instance.OnStepCompleted(currentPosition - new Vector2(0f, 0.5f));
                TriggerExecutor.Instance.OnStepStarted(currentPosition + facingDirection - new Vector2(0f, 0.5f));
            }

            if (ty + 2*tj >= 2f && !jumpPhase2) {
                jumpPhase2 = true;
                Debug.Log("[PlayerMoveController3] Jump Phase 2");
                
                currentPosition = (jumpStartPos + targetPosY) / 2f + (1 - ty) * facingDirection;
                Debug.Log("Currnent Position: " + currentPosition.ToString());
                if (2*tj >= 2f) isJumping = false;
                TriggerExecutor.Instance.OnStepCompleted(currentPosition - new Vector2(0f, 0.5f));
                if (elapsedTimeY != 0f) TriggerExecutor.Instance.OnStepStarted(currentPosition + facingDirection - new Vector2(0f, 0.5f));
            }

            if (2*tj >= 2f)
            {
                transform.position = new Vector2(transform.position.x, targetPosY.y);
                isJumping = false;
                jumpPhase1 = false;
                jumpPhase2 = false;

                queuedJump = 0;
                elapsedTimeJump = 0f;
                
                syncCanMoveXY(currentPosition);
                syncCanMoveYX(currentPosition);

                if (elapsedTimeY != 0f)
                {
                    targetPosY += (1 - ty) * moveDistance * facingDirection;
                    startPosY = currentPosition;
                    currentPosition.y = targetPosY.y;

                    Debug.Log("start Position: " + startPosY.ToString());
                    Debug.Log("target Position: " + targetPosY.ToString());
                    Debug.Log("Current Position: " + currentPosition.ToString());
                    isMovingY = true;
                }
                
                Debug.Log("[PlayerMoveController3] Jump Complete");
            }
        }
    }

    /// <summary>
    /// 0->1 : 
    /// </summary>
    private float GetJumpPos(float x)
    {
        return 2f*x - 4*jumpHeight*Mathf.Sin(Mathf.Deg2Rad*rho)*x*(x-1);
    }

    private void EnqueueJump()
    {
        if (!isMovingX && !isJumping && lastInputManager.GetKeyDownJump())
        {
            if (facingDirection == Vector2.up)
            {
                queuedJump = 1;
            }
            else if (facingDirection == Vector2.down)
            {
                queuedJump = -1;
            }
        }
    }

    private void StartJump()
    {
        if (facingDirection != Vector2.up && facingDirection != Vector2.down)
        {
            queuedJump = 0;
            return;
        }
        isJumping = true;
        jumpPhase1 = false;
        jumpPhase2 = false;
        elapsedTimeJump = 0f;
        queuedJump = 0;
        isMovingY = false;

        jumpStartPos = transform.position;
        targetPosY = jumpStartPos + facingDirection * 2;
        
        if (elapsedTimeY == 0f)
        {
            TriggerExecutor.Instance.OnStepStarted((jumpStartPos + targetPosY) / 2f - new Vector2(0f, 0.5f));
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
        //check = MapManager.Instance.IsCollision(from - new Vector3(0f, 0.5f, 0f) + dir * moveDistance - gridPreset)
        //    || (hit.collider != null && hit.collider.CompareTag("Interactable"));
        check = hit.collider != null;

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

    /// <summary>
    /// 플레이어의 현재 층(floor)을 설정합니다.
    /// </summary>
    public void SetFloor(int newFloor)
    {
        if (newFloor >= 1 && newFloor <= 3)
        {
            layer = newFloor;
            Debug.Log("[PlayerMoveController3] Set Floor : " + newFloor);
        }
    }

    /// <summary>
    /// 플레이어의 현재 층(floor) 번호를 반환합니다.
    /// </summary>
    public int GetCurrentFloor()
    {
        if (floorLayer == LayerMask.GetMask("Col 1F")) return 1;
        else if (floorLayer == LayerMask.GetMask("Col 2F")) return 2;
        else if (floorLayer == LayerMask.GetMask("Col 3F")) return 3;
        else return 1;
    }

    public bool GetIsJumping()
    {
        return isJumping;
    }

    public IEnumerator Teleport(Vector2Int pos, float time = 1f)
    {
        // Move Reset
        isMovingX = false;
        isMovingY = false;
        elapsedTimeX = 0f;
        elapsedTimeY = 0f;
        nextInputX = true;
        nextInputY = true;

        // Jump Reset
        isJumping = false;
        elapsedTimeJump = 0f;
        jumpPhase1 = false;
        jumpPhase2 = false;

        // Ignore Input & Wait
        teleporting = true;
        lastInputManager.IgnoreInput(time + .5f);
        queuedDirectionX = Vector2.zero;
        queuedDirectionY = Vector2.zero;
        queuedJump = 0;
        yield return new WaitForSeconds(time);
        
        // Teleport
        transform.position = new Vector3(pos.x + 0.5f, pos.y, 0);
        currentPosition = transform.position;
        teleporting = false;
    }
}