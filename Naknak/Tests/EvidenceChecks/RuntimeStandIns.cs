// These checks execute the production investigation and inventory code.
// Unity rendering, input and scene transitions are deliberately outside this console fixture.
using System.Collections;
using Naknak.Inventory;

namespace UnityEngine
{
    public sealed class SerializeField : Attribute { }
    public class MonoBehaviour
    {
        private readonly List<IEnumerator> routines = new();
        public bool isActiveAndEnabled = true;
        public string name = "Witness";
        public GameObject gameObject = new();
        public void StartCoroutine(IEnumerator routine) { routines.Add(routine); routine.MoveNext(); }
        public void AdvanceCoroutines()
        {
            foreach (var routine in routines.ToArray()) if (!routine.MoveNext()) routines.Remove(routine);
        }
    }
    public sealed class GameObject
    {
        public Scene scene = new();
        public T GetComponentInChildren<T>(bool includeInactive) where T : class => null;
    }
    public sealed class Scene { public GameObject[] GetRootGameObjects() => Array.Empty<GameObject>(); }
    public sealed class SpriteRenderer { public bool enabled; }
    public static class Debug { public static void LogError(string text, object context) => throw new Exception(text); }
}

public interface IInteractable { void Interact(); }
public sealed class ConditionID { public string name; }
public enum CompareType { Equal }
public enum ActionType { Set }
public struct DialogCondition { public ConditionID id; public CompareType compare; public int value; }
public struct DialogAction { public ConditionID id; public ActionType type; public int value; }
public sealed class ConditionManager
{
    public static ConditionManager Instance = new();
    private readonly Dictionary<string, int> values = new();
    public int Get(ConditionID id) => values.TryGetValue(id.name, out var value) ? value : 0;
    public void Set(ConditionID id, int value) => values[id.name] = value;
    public void ExecuteActions(List<DialogAction> actions) { foreach (var action in actions) Set(action.id, action.value); }
}
public sealed class SceneFlowContainer
{
    public sealed class Connection { public string id; public List<DialogCondition> conditions = new(); }
    public List<Connection> connections = new();
    public static bool Passes(List<DialogCondition> conditions) => conditions == null ||
        conditions.All(c => c.id != null && ConditionManager.Instance.Get(c.id) == c.value);
}
public sealed class SceneLoadManager
{
    public static SceneLoadManager Instance = new();
    public static bool IsPreparingMemory, IsRestoringSession, IsCutscene, IsLoading;
    public static bool IsMemorySpace = true;
    public SceneFlowContainer.Connection Travelled;
    public bool Travel(SceneFlowContainer flow, SceneFlowContainer.Connection connection)
    { Travelled = connection; return true; }
}
public enum GameState { Gameplay, Pause }
public sealed class GameStateManager { public static GameStateManager Instance = new(); public GameState GameState; }
public sealed class StoryData { public string name; }
public sealed class DialogManager
{
    public static DialogManager Instance = new();
    public bool CanShowDialog = true, IsDialogOpen;
    public StoryData CurrentStory;
    public void ShowDialog(StoryData story) { CurrentStory = story; IsDialogOpen = true; }
}
public sealed class TutorialScenePresentation { public void SetUnknownSpace(bool active) { } }
public sealed class GameMenuController
{
    public static GameMenuController Instance = new();
    public static InventoryModel Inventory;
    public static bool IsBlockingGameplay;
    public Action<InventoryItem> Selected;
    private Action cancelled;
    public bool RejectSelection;
    public bool SelectInventoryItem(string prompt, Action<InventoryItem> selected, Action onCancelled, bool importantItems = true)
    {
        if (RejectSelection) return false;
        Selected = selected; cancelled = onCancelled;
        IsBlockingGameplay = true; GameStateManager.Instance.GameState = GameState.Pause; return true;
    }
    public void Choose(InventoryItem item)
    {
        var callback = Selected; Resume(); callback(item);
    }
    public void Cancel() { var callback = cancelled; Resume(); callback(); }
    private void Resume()
    {
        Selected = null; cancelled = null; IsBlockingGameplay = false;
        GameStateManager.Instance.GameState = GameState.Gameplay;
    }
}
