using UnityEngine;

public class IdleState : IState
{
    PlayerFSM fsm;

    // 생성자
    public IdleState(PlayerFSM fsm)
    {
        this.fsm = fsm;
    }

    public void Enter()
    {
        fsm.Anim.SetBool("isMoving", false);
    }

    public void Execute()
    {
        
    }

    public void Exit()
    {

    }

}
