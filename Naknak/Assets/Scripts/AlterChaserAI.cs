using System.Collections;
using UnityEngine;

public class AlterChaserAI : MonoBehaviour
{
    [Header("Chase Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float moveDelay = 0.2f;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float hitDistance = 0.35f;
    [SerializeField] private bool destroyOnHit = false;

    private Transform target;
    private bool isChasing = false;
    private bool isMoving = false;
    private bool alreadyHit = false;

    public void StartChase(Transform player)
    {
        target = player;
        isChasing = true;
        alreadyHit = false;
        StopAllCoroutines();
        StartCoroutine(ChaseLoop());
    }

    private IEnumerator ChaseLoop()
    {
        while (isChasing && target != null)
        {
            if (!isMoving)
            {
                Vector3 nextPos = GetNextTilePosition();
                yield return StartCoroutine(MoveOneTile(nextPos));
                CheckHitByDistance();
                yield return new WaitForSeconds(moveDelay);
            }
            yield return null;
        }
    }

    private Vector3 GetNextTilePosition()
    {
        Vector3 diff = target.position - transform.position;
        diff.z = 0f;
        Vector3 moveDir = Vector3.zero;
        float absX = Mathf.Abs(diff.x);
        float absY = Mathf.Abs(diff.y);

        if (absX >= absY)
        {
            if (diff.x > 0f) moveDir = Vector3.right;
            else if (diff.x < 0f) moveDir = Vector3.left;
        }
        else
        {
            if (diff.y > 0f) moveDir = Vector3.up;
            else if (diff.y < 0f) moveDir = Vector3.down;
        }

        Vector3 nextPos = transform.position + moveDir * tileSize;
        nextPos.z = transform.position.z;
        return nextPos;
    }

    private IEnumerator MoveOneTile(Vector3 targetPos)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, targetPos);
        if (distance <= 0.01f)
        {
            isMoving = false;
            yield break;
        }
        float elapsed = 0f;
        float duration = distance / moveSpeed;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
    }

    private void CheckHitByDistance()
    {
        if (alreadyHit || target == null) return;
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= hitDistance)
        {
            alreadyHit = true;
            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
