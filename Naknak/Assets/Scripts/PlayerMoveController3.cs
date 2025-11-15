using UnityEngine;

// enum Direction 은 PlayerMoveController 의 enum 을 따릅니다.

public class PlayerMoveController3 : MonoBehaviour
{
    public float moveDuration = 0.3f;
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float sameInputTime = 0.7f;
    Animator anim;
    LastInputManager lastInputManager;

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
    internal Vector2 currentDirectionX;
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
    internal Vector2 currentDirectionY;
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

        gridPreset = MapManager.Instance.World2Grid(transform.position);
    }

    void Update()
    {
        UpdateMoveX();
        UpdateMoveY();

        Debug.Log("canMove XY" + canMoveXY[0] + " " + canMoveXY[1]);
        Debug.Log("canMove YX" + canMoveYX[0] + " " + canMoveYX[1]);

        isMoving = isMovingX || isMovingY;

        // 플레이어 애니메이션
        // X, Y 완전히 동시에 눌리면 X 우선순위
        if (xFirst && !isMovingX) xFirst = false;
        if (yFirst && !isMovingY) yFirst = false;

        if (!yFirst && isMovingX) xFirst = true;
        else if (!xFirst && isMovingY) yFirst = true;

        if (xFirst && isMovingX) anim.SetFloat("direction", (float)vector2Dir(currentDirectionX));
        else if (yFirst && isMovingY) anim.SetFloat("direction", (float)vector2Dir(currentDirectionY));
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

                canMoveXY[0] = !checkCollider(targetPosX, Vector2.up);
                canMoveXY[1] = !checkCollider(targetPosX, Vector2.down);

                if (!isMovingY)
                {
                    canMoveYX[0] = !checkCollider(targetPosX, Vector2.left);
                    canMoveYX[1] = !checkCollider(targetPosX, Vector2.right);
                }

                if (queuedDirectionX != Vector2.zero) StartMoveX();
                else isMovingX = false;
            }
        }
    }

    private void EnqueueMoveX()
    {
        float moveValue = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(moveValue) == 1f)
        {
            queuedDirectionX = new Vector2(moveValue, 0f);
        }
    }

    private void StartMoveX()
    {
        currentDirectionX = queuedDirectionX;
        startPosX = currentPosition;

        // 수직 방향 충돌 계산
        canMoveXY[0] = !checkCollider(currentPosition, Vector2.up);
        canMoveXY[1] = !checkCollider(currentPosition, Vector2.down);

        bool diagonalBlocked = false;
        if (queuedDirectionX == Vector2.left && !canMoveYX[0])
        {
            diagonalBlocked = true;
        }
        else if (queuedDirectionX == Vector2.right && !canMoveYX[1])
        {
            diagonalBlocked = true;
        }

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

                canMoveYX[0] = !checkCollider(targetPosY, Vector2.left);
                canMoveYX[1] = !checkCollider(targetPosY, Vector2.right);
                if (!isMovingX)
                {
                    canMoveXY[0] = !checkCollider(targetPosY, Vector2.up);
                    canMoveXY[1] = !checkCollider(targetPosY, Vector2.down);
                }

                if (queuedDirectionY != Vector2.zero) StartMoveY();
                else isMovingY = false;
            }
        }
    }

    private void EnqueueMoveY()
    {
        float moveValue = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(moveValue) == 1f)
        {
            queuedDirectionY = new Vector2(0f, moveValue);
        }
    }

    private void StartMoveY()
    {
        currentDirectionY = queuedDirectionY;
        startPosY = currentPosition;

        canMoveYX[0] = !checkCollider(currentPosition, Vector2.left);
        canMoveYX[1] = !checkCollider(currentPosition, Vector2.right);

        bool diagonalBlocked = false;
        if (queuedDirectionY == Vector2.up && !canMoveXY[0])
        {
            diagonalBlocked = true;
        }
        else if (queuedDirectionY == Vector2.down && !canMoveXY[1])
        {
            diagonalBlocked = true;
        }

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

    bool checkCollider(Vector3 from, Vector3 dir)
    {
        return MapManager.Instance.IsCollision(from - new Vector3(0f, 0.5f, 0f) + dir * moveDistance - gridPreset);
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
}