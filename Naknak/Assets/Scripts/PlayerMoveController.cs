using UnityEngine;
using System.Collections;
using System;

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
    [SerializeField] private float moveDistance = 1f;
    Rigidbody2D rb;
    Animator anim;

    float moveX = 0f;
    float moveY = 0f;
    Direction dir = Direction.Down; // Default Direction
    Vector3 queuedDir = Vector3.zero;

    Vector2 targetPos;
    bool isMoving = false;

      Vector2 SnapToInteger(Vector2 pos)
        {
            float snappedX = Mathf.Round(pos.x)+0.5f;
            float snappedY = Mathf.Round(pos.y);
            return new Vector2(snappedX, snappedY);
        }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.position = SnapToInteger(rb.position);
        targetPos = rb.position;
    }

    void Update()
    {
        // Input on update
        moveX = Input.GetAxisRaw("Horizontal"); // -1 ~ 1
        moveY = Input.GetAxisRaw("Vertical");   // -1 ~ 1
    }


    void FixedUpdate()
    {

        rb.linearVelocity = Vector2.zero;
        if (isMoving)
        {

            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            anim.SetBool("isMoving", true);

            if (Vector2.Distance(rb.position, targetPos) <= 0.001f)
            {
                rb.MovePosition(SnapToInteger(targetPos));
                targetPos = SnapToInteger(targetPos);
                isMoving = false;
            }
            anim.SetFloat("direction", (float)dir);

        }
        else
        {
            anim.SetBool("isMoving", false);

        }


        if (Mathf.Abs(moveX) == 1f)
        {
            float sx = Mathf.Sign(moveX);
            targetPos = rb.position + new Vector2(sx * moveDistance, 0f);
            dir = (sx > 0f) ? Direction.Right : Direction.Left;
            isMoving = true;
        }
        if (Mathf.Abs(moveY) == 1f)
        {
            float sy = Mathf.Sign(moveY);
            targetPos = rb.position + new Vector2(0f, sy * moveDistance);
            dir = (sy > 0f) ? Direction.Up : Direction.Down;
            isMoving = true;
        }
    }
}
