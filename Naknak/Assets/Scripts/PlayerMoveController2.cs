using UnityEngine;

// enum Direction 은 PlayerMoveController 의 enum 을 따릅니다.

public class PlayerMoveController2 : MonoBehaviour
{
    public float moveDuration = 0.3f; // (1 / 1타일 이동 시간)
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float sameInputTime = 0.7f;
    Animator anim;

    Vector2 startPos;
    Vector2 targetPos;
    Vector2 currentDirection;

    Vector2 _queuedDirection; // backing field
    Vector2 queuedDirection { 
        get { return _queuedDirection; }
        set
        {
            if (value != currentDirection || nextInput) _queuedDirection = value;
        }
    }

    private bool isMoving // animator의 parameter와 연동
    {
        get { return anim.GetBool("isMoving"); }
        set { anim.SetBool("isMoving", value); }
    }

    bool nextInput = false;
    float elapsedTime = 0f;


    void Start()
    {
        anim = GetComponent<Animator>();
        targetPos = transform.position;
        startPos = transform.position;
    }

    void Update()
    {
        // 입력 Enqueue는 상시
        enqueueMove();

        // 입력 큐에 방향이 들어오면 해당 방향으로 이동 시작
        if (!isMoving && queuedDirection != Vector2.zero)
        {
            startMove();
        }

        // 이동 알고리즘
        if (isMoving)
        {
            // 이동 타이머
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration); // t 값이 0~1로 이동하며 move progress
            transform.position = Vector2.Lerp(startPos, targetPos, t);

            // 같은 키 입력 배제 시간
            if (t >= sameInputTime) nextInput = true;

            // 해당 칸에 거의 근접했을 때, 위치 고정 및 이동 완료
            if (Vector2.Distance(transform.position, targetPos) <= 0.001f)
            {
                transform.position = targetPos;
                startPos = targetPos;

                // 대기 큐가 있으면 멈추지 않고 다시 이동 시작, 없으면 정지
                if (queuedDirection != Vector2.zero) startMove();
                else isMoving = false;
            }
        }
    }

    void enqueueMove()
    {
        float moveX = Input.GetAxisRaw("Horizontal"); // -1 ~ 1
        float moveY = Input.GetAxisRaw("Vertical");   // -1 ~ 1

        if (Mathf.Abs(moveX) == 1f && moveY == 0f)
        {
            queuedDirection = new(moveX, 0f);
        }
        else if (moveX == 0f && Mathf.Abs(moveY) == 1f)
        {
            queuedDirection = new(0f, moveY);
        }
    }

    void startMove()
    {
        /*
            아마 여기 어딘가에 이동 가능한 위치인지 판별하는 로직이 들어가지 않을까요
        */

        currentDirection = queuedDirection; // 다음 방향 저장

        targetPos += currentDirection * moveDistance; // 목표 위치 설정
        startPos = transform.position; // 시작 위치 저장 (Lerp 함수 사용 위함)

        anim.SetFloat("direction", (float)vector2Dir(currentDirection)); // 애니메이터 방향 연동

        isMoving = true;
        nextInput = false;
        elapsedTime = 0f;

        queuedDirection = Vector2.zero; // Clear Queue
    }

    Direction vector2Dir(Vector2 vec)
    {
        if (vec.y == 1f) return Direction.Up;
        if (vec.x == 1f) return Direction.Right;
        if (vec.y == -1f) return Direction.Down;
        if (vec.x == -1f) return Direction.Left;
        return Direction.Down; // 기본값: 아래 방향
    }
}
