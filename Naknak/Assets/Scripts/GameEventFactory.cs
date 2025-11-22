/// <summary>
/// 팩토리 패턴으로 GameEvent의 인스턴스를 제공합니다.
/// 추후 제너릭으로 변경합니다.
/// </summary>
public static class GameEventFactory
{
    public static GameEventBase CreateDialogEvent(string storyID)
    {
        return new DialogEvent(storyID);
    }

    public static GameEventBase CreateNextDialogEvent()
    {
        return new NextDialogEvent();
    }

    public static GameEventBase CreateGameStateChangeEvent(GameState gameState)
    {
        return new GameStateChangeEvent(gameState);
    }
}
