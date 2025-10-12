using System.Collections.Generic;
using UnityEngine;

// enum Direction 은 PlayerMoveController 의 enum 을 따릅니다.

public class PlayerMoveController3 : MonoBehaviour
{
    #region Define AxisMoveController Class
    /// <summary>
    /// X축과 Y축 이동 로직 통합 컨트롤러 클래스
    /// </summary>
    class AxisMoveController
    { 
        // 어느 축을 담당할지 설정하는 열거형
        internal enum Axis { X, Y }
        private readonly Axis moveAxis;
        private readonly string inputAxisName;

        static internal PlayerMoveController3 owner;

        // 이동 관련 내부 변수
        private Vector2 startPos;
        private Vector2 targetPos;
        internal Vector2 currentDirection;

        private Vector2 _queuedDirection;
        private Vector2 queuedDirection
        {
            get { return _queuedDirection; }
            set
            {
                if (value != currentDirection || nextInput) _queuedDirection = value;
            }
        }

        private bool nextInput = false;
        internal bool isMoving = false;
        private float elapsedTime = 0f;

        // 생성자에서 어느 축을 담당할지 전달
        public AxisMoveController(Axis axis)
        {
            moveAxis = axis;
            // 축 이름 설정 (LastInputManager에 사용)
            inputAxisName = (moveAxis == Axis.X) ? "Horizontal" : "Vertical"; // X, Y
            startPos = owner.transform.position;
            targetPos = owner.transform.position;
        }

        public void Update()
        {
            enqueueMove();

            if (!isMoving && queuedDirection != Vector2.zero)
            {
                startMove();
            }

            if (isMoving)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.Clamp01(elapsedTime / owner.moveDuration);


                Vector2 newPos;
                if (moveAxis == Axis.X) // X, Y
                {
                    newPos = new Vector2(Mathf.Lerp(startPos.x, targetPos.x, t), owner.transform.position.y);
                }
                else // Axis.Y
                {
                    newPos = new Vector2(owner.transform.position.x, Mathf.Lerp(startPos.y, targetPos.y, t));
                }
                owner.transform.position = newPos;

                if (t >= owner.sameInputTime) nextInput = true;

                // 축에 따라 도착 여부 체크 및 위치 Snap
                bool arrived = false;
                if (moveAxis == Axis.X) // X, Y
                {
                    if (Mathf.Abs(owner.transform.position.x - targetPos.x) <= 0.001f)
                    {
                        owner.transform.position = new Vector2(targetPos.x, owner.transform.position.y);
                        arrived = true;
                    }
                }
                else // Axis.Y
                {
                    if (Mathf.Abs(owner.transform.position.y - targetPos.y) <= 0.001f)
                    {
                        owner.transform.position = new Vector2(owner.transform.position.x, targetPos.y);
                        arrived = true;
                    }
                }

                if (arrived)
                {
                    startPos = targetPos;

                    if (queuedDirection != Vector2.zero) startMove();
                    else isMoving = false; // 해당 축 이동 완료
                }
            }
        }

        private void enqueueMove()
        {
            float moveValue = owner.lastInputManager.GetAxisRaw(inputAxisName);

            if (Mathf.Abs(moveValue) == 1f)
            {
                queuedDirection = (moveAxis == Axis.X) ? new Vector2(moveValue, 0f) : new Vector2(0f, moveValue); // X, Y
            }
        }

        private void startMove()
        {
            currentDirection = queuedDirection;
            startPos = owner.currentPosition;

            RaycastHit2D hit = Physics2D.Raycast(startPos - new Vector2(0, 0.5f), currentDirection, owner.moveDistance, owner.layer);
            // Ray draw
            Debug.DrawRay(startPos - new Vector2(0, 0.5f), currentDirection * owner.moveDistance, Color.red);

            if (hit.collider != null)
            {
                if (!owner.isMoving) owner.anim.SetFloat("direction", (float)owner.vector2Dir(currentDirection));
                isMoving = false;
                nextInput = true;
                elapsedTime = 0f;
                queuedDirection = Vector2.zero;
            }
            else
            {
                targetPos = startPos + currentDirection * owner.moveDistance;
                owner.currentPosition = targetPos;

                isMoving = true;
                nextInput = false;
                elapsedTime = 0f;
                queuedDirection = Vector2.zero;
            }
        }
    }
    #endregion

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
    
    private AxisMoveController moveCtrlX;
    private AxisMoveController moveCtrlY;
    void Start()
    {
        anim = GetComponent<Animator>();
        lastInputManager = GetComponent<LastInputManager>();
        // X축과 Y축을 각각 담당하도록 지정하여 인스턴스 생성
        AxisMoveController.owner = this;
        moveCtrlX = new(AxisMoveController.Axis.X);
        moveCtrlY = new(AxisMoveController.Axis.Y);
        currentPosition = transform.position;
    }

    void Update()
    {
        moveCtrlX.Update();
        moveCtrlY.Update();

        isMoving = moveCtrlX.isMoving || moveCtrlY.isMoving;

        // 플레이어 애니메이션
        // X, Y 완전히 동시에 눌리면 X 우선순위
        if (xFirst && !moveCtrlX.isMoving) xFirst = false;
        if (yFirst && !moveCtrlY.isMoving) yFirst = false;

        if (!yFirst && moveCtrlX.isMoving) xFirst = true;
        else if (!xFirst && moveCtrlY.isMoving) yFirst = true;

        if (xFirst && moveCtrlX.isMoving) anim.SetFloat("direction", (float)vector2Dir(moveCtrlX.currentDirection));
        else if (yFirst && moveCtrlY.isMoving) anim.SetFloat("direction", (float)vector2Dir(moveCtrlY.currentDirection));
          
    }

    Direction vector2Dir(Vector2 vec)
    {
        if (vec.y == 1f) return Direction.Up;
        if (vec.x == 1f) return Direction.Right;
        if (vec.y == -1f) return Direction.Down;
        if (vec.x == -1f) return Direction.Left;
        return Direction.Down;
    }
}