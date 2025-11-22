public class GameStateChangeEvent : GameEventBase
{
    private GameState _gameState;

    public GameStateChangeEvent(GameState gameState)
    {
        _gameState = gameState;
    }

    public override void Execute()
    {
        GameStateManager.Instance.ChangeGameState(_gameState);
    }
}
