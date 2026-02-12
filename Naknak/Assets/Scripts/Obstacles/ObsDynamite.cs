using UnityEngine;

public class ObsDynamite : ObstacleBase
{
    public override bool RunObstacleAction(bool isJumping = false)
    {
        if (isJumping) return true;
        Debug.Log("펑!!!!!");
        Destroy(gameObject);
        return false;
    }
    public override void LoadOnCamera()
    {
        Debug.Log("다이너마이트 자연 소멸");
        Destroy(gameObject);
    }
}
