using UnityEngine;

public class MoveState : IState
{
    PlayerFSM fsm;
    public MoveState(PlayerFSM fsm)
    {
        this.fsm = fsm;
    }

    private Vector2 startPos;
    private Vector2 targetPos;

    private Vector2 currentDirection;
    private Vector2 queuedDirection;

    private bool nextInput = false;
    private float elapsedTime = 0f;
    private float moveDuration = 0.3f;
    private float sameInputTime = 0.7f;

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
