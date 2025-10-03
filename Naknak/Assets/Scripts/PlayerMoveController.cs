using UnityEngine;

// up: 0, right: 1, down: 2, left: 3
enum Direction
{
    Up,
    Right,
    Down,
    Left
}

public class PlayerMoveController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    Rigidbody2D rb;
    Animator anim;

    float moveX = 0f;
    float moveY = 0f;
    Direction dir = Direction.Down; // Default Direction

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Input on update
        moveX = Input.GetAxis("Horizontal"); // -1 ~ 1
        moveY = Input.GetAxis("Vertical");   // -1 ~ 1
    }

    void FixedUpdate()
    {
        Vector2 movement = new Vector2(moveX, moveY) * speed;
        rb.linearVelocity = movement;

        if (Mathf.Abs(moveX) > .1f || Mathf.Abs(moveY) > .1f) // 임계 속도 (실수 속도에 의함)
        {
            anim.SetBool("isMoving", true);

            // 움직이면 방향 조정 (X 방향의 움직임 우선 적용)
            if (Mathf.Abs(moveX) >= Mathf.Abs(moveY)) { 
                dir = (moveX > 0) ? Direction.Right : Direction.Left;
            } else {
                dir = (moveY > 0) ? Direction.Up : Direction.Down;
            }
            
            anim.SetFloat("direction", (float)dir);
        }
        else {
            anim.SetBool("isMoving", false);
        }
    }
}
