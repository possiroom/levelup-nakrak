public interface IState
{
    // 1. 상태 진입 시 한 번 실행
    void Enter();

    // 2. 상태 중 매 프레임 실행, (탈출 조건 포함)
    void Execute();

    // 3. 상태 탈출 시 한 번 실행
    void Exit();
}
