using UnityEngine;

public class ObsWarning : ObstacleExecution
{
    [SerializeField, Tooltip("경고가 보이는 시간")] 
    private float firstAttackTime = 1.5f;

    private float leftTime;


    protected override void FirstExecute()
    {
        // 오른쪽으로 날아감
        leftTime = firstAttackTime;
        FollowPlayerPosition();
    }

    private void Update()
    {
        leftTime -= Time.deltaTime;
        FollowPlayerPosition();

        if (leftTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void FollowPlayerPosition()
    {
        transform.position = PlayerStatus.Instance.GetPlayerTransform().position + Vector3.up * 1.5f;
    }
}
