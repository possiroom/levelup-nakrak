using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    // Singleton
    public static GameEventManager Instance { get; private set;}
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

    public void Submit(GameEventBase evt){
        evt.Execute();
    }
}
