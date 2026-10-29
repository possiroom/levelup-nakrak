// Only copied into the isolated loader verification project. Gameplay/UI dependencies
// are small stand-ins; SceneLoadManager, SceneFlowContainer and DialogManager are real.
using System.Collections.Generic;
using UnityEngine;

public sealed class GameMenuController
{
    public static GameMenuController Instance;
    public static Naknak.Inventory.InventoryModel Inventory;
    public static bool BlockingForTest;
    public static bool IsBlockingGameplay => BlockingForTest || SceneLoadManager.IsLoading;
    public System.Action<Naknak.Inventory.InventoryItem> Selection;
    public bool SelectInventoryItem(string prompt, System.Action<Naknak.Inventory.InventoryItem> selected,
        System.Action cancelled = null, bool importantItems = true)
    {
        if (IsBlockingGameplay) return false;
        Selection = selected;
        BlockingForTest = true;
        return true;
    }
    public void ChooseForTest(Naknak.Inventory.InventoryItem item)
    {
        var callback = Selection;
        Selection = null;
        BlockingForTest = false;
        callback?.Invoke(item);
    }
    public void CancelForTest() { Selection = null; BlockingForTest = false; }
}
public enum GameState { Gameplay, Dialog, Pause }
public sealed class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public GameState GameState;
    private void Awake() { Instance = this; }
    public void ChangeGameState(GameState state) { GameState = state; }
}
public sealed class GameEventBase { public GameState state; public StoryData story; }
public static class GameEventFactory
{
    public static GameEventBase CreateGameStateChangeEvent(GameState state) => new GameEventBase { state = state };
    public static GameEventBase CreateDialogEvent(StoryData story) => new GameEventBase { story = story };
}
public sealed class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance;
    private void Awake() { Instance = this; }
    public void Submit(GameEventBase evt)
    {
        if (evt.story != null) DialogManager.Instance.ShowDialog(evt.story);
        else GameStateManager.Instance.ChangeGameState(evt.state);
    }
}
public sealed class DialogViewer : MonoBehaviour
{
    public void ChangeName(string text) { }
    public void ChangeActivateImage(int value) { }
    public void ActivateEndMark(bool value) { }
    public void ChangeStoryText(string text) { }
}
public sealed class DialogSelectViewer : MonoBehaviour
{
    public void SelectDeactivate() { }
    public void SelectActivate(List<StorySelectData> data) { }
    public void ChangeSelectIndex(int index) { }
}
public sealed class PlayerMoveController3 : MonoBehaviour
{ public void TeleportToWorld(Vector2 value) { transform.position = value; } }
public sealed class ConditionToEvent : MonoBehaviour { }
public sealed class TriggerExecutor : MonoBehaviour
{
    public static TriggerExecutor Instance;
    public GridLayout grid;
    private void Awake() { Instance = this; }
    public void ChangeMap(GridLayout value, GameObject map) { grid = value; }
    public void ChangeC2E(ConditionToEvent value) { }
    public void AfterDialogEvent(StoryData story) { }
}
public interface IInteractable { void Interact(); }
public sealed class ReadOnlyAttribute : PropertyAttribute { }
[System.Serializable] public sealed class ConditionValue { public string key; public int value; }
