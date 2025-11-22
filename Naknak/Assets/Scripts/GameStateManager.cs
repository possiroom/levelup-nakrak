using UnityEngine;

public enum GameState
{
    Gameplay, // 평소 상태
    Dialog,   // 대화 중
    Pause     // 일시 정지
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState GameState { get; private set; }
    
    // Singleton
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ChangeGameState(GameState gameState)
    {
        GameState = gameState;
        Debug.Log("[GameStateManager] Change Game State: " + gameState.ToString());
    }
}
