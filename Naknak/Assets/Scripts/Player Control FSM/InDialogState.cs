using UnityEngine;
/// <summary>
/// Idle State와 차이점은 입력 자체를 무시하여 Move State로 넘어갈 가능성을 없앨 수 있음
/// </summary>
public class InDialogState : IState
{
    PlayerFSM fsm;

    public InDialogState(PlayerFSM fsm)
    {
        this.fsm = fsm;
    }

    public void Enter()
    {

    }

    public void Execute()
    {

    }

    public void Exit()
    {

    }
}
