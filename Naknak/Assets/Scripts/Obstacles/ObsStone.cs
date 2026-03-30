using UnityEngine;

public class ObsStone : ObstacleExecution
{
    [SerializeField, Tooltip("돌이 날라와 처음 공격하는 시간")] 
    private float firstAttackTime = 1f;

    [SerializeField, Tooltip("첫 공격 기준 돌이 날아오는 칸")]
    private int distance = 8;
    private float leftDistance;


    protected override void FirstExecute()
    {
        // 오른쪽으로 날아감
        leftDistance = distance;
    }

    private void Update()
    {
        float moveStep = Time.deltaTime * distance / firstAttackTime;
        transform.position += Vector3.right * moveStep;
        leftDistance -= moveStep;

        if (-2f <= leftDistance && leftDistance <= 0f)
        {
            PlayerStatus.Instance.TryDamageStone(transform.position.y);
        }
        else if (leftDistance <= -12f)
        {
            Destroy(gameObject);
        }
    }
}
