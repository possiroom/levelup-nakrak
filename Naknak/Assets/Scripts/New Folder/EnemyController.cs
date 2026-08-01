using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private LayerMask wallLayer;

    private Transform player;
    private GridPathfinder pathfinder;

    private bool isMoving = false;

    private void Start()
    {
        pathfinder = new GridPathfinder(wallLayer);
    }

    public void Initialize()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null)
            return;

        if (isMoving)
            return;

        MoveOneStep();
    }

    private void MoveOneStep()
    {
        Vector2Int start =
            Vector2Int.RoundToInt(transform.position);

        Vector2Int goal =
            Vector2Int.RoundToInt(player.position);

        if (pathfinder.TryGetNextStep(start, goal, out Vector2Int nextStep))
        {
            StartCoroutine(Move(nextStep));
        }
    }

    private IEnumerator Move(Vector2Int nextCell)
    {
        isMoving = true;

        Vector3 target =
            new Vector3(nextCell.x, nextCell.y, transform.position.z);

        while (Vector2.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime);

            yield return null;
        }

        transform.position = target;

        isMoving = false;
    }
}