using System;
using Naknak.Inventory;

public sealed partial class GameMenuController
{
    private Action<InventoryItem> itemSelected;
    private Action itemSelectionCancelled;
    private string itemSelectionPrompt;

    // Reuse the inventory window; ordinary inventory browsing keeps its existing behavior.
    public bool SelectInventoryItem(string prompt, Action<InventoryItem> onSelected, Action onCancelled = null,
        bool importantItems = true)
    {
        if (onSelected == null || IsBlockingGameplay || page != Page.Closed) return false;
        if (GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return false;
        itemSelected = onSelected;
        itemSelectionCancelled = onCancelled;
        itemSelectionPrompt = prompt;
        keyItems = importantItems;
        selected = 0;
        Pause();
        ShowPage(Page.Items);
        return true;
    }

    private void ActivateInventoryItem(int index)
    {
        Focus(index);
        if (itemSelected == null || index >= visibleItems.Count) return;
        var item = visibleItems[index];
        var callback = itemSelected;
        ClearItemSelection();
        Resume();
        callback(item);
    }

    private void ReturnFromItems()
    {
        if (itemSelected == null) { ShowPage(Page.Menu); return; }
        var callback = itemSelectionCancelled;
        ClearItemSelection();
        Resume();
        callback?.Invoke();
    }

    private void ClearItemSelection()
    {
        itemSelected = null;
        itemSelectionCancelled = null;
        itemSelectionPrompt = null;
    }
}
