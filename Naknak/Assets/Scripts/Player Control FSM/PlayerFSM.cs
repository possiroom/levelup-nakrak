using UnityEngine;

public class PlayerFSM : MonoBehaviour
{
    // 현재 상태
    public IState CurrentState { get; private set; }

    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public InDialogState InDialogState { get; private set; }

    // 공용 컴포넌트
    public LastInputManager InputManager { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public Animator Anim { get; private set; }

    // 변수
    public float moveDuration = 0.3f;

    public Vector2 direction;

    void Start()
    {
        direction = Vector2.down;

        InputManager = GetComponent<LastInputManager>();
        PlayerTransform = GetComponent<Transform>();
        Anim = GetComponent<Animator>();
        
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        InDialogState = new InDialogState(this);

        ChangeState(IdleState);
    }

    void Update()
    {
        CurrentState?.Execute();
    }

    void ChangeState(IState state)
    {
        // 기존 상태가 있으면 Exit
        CurrentState?.Exit();
        
        // 교체 후 실행
        CurrentState = state;
        CurrentState.Enter();
    }
}
