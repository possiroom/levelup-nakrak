using System;
using Naknak.Inventory;

public sealed partial class GameMenuController
{
    private Action<InventoryItem> itemSelected;
    private Action itemSelectionCancelled;
    private string itemSelectionPrompt;
    private bool completionSavePending;

    // Reuse the inventory window; ordinary inventory browsing keeps its existing behavior.
    public bool SelectInventoryItem(string prompt, Action<InventoryItem> onSelected, Action onCancelled = null,
        bool importantItems = true)
    {
        if (onSelected == null || IsBlockingGameplay || page != Page.Closed) return false;
        if (GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return false;
        itemSelected = onSelected;
        itemSelectionCancelled = onCancelled;
        itemSelectionPrompt = prompt;
        inventoryCategory = importantItems ? 2 : 0;
        selected = 0;
        Pause();
        ShowPage(Page.Items);
        return true;
    }

    private void ActivateInventoryItem(int index)
    {
        Focus(index);
        if (index < 0 || index >= visibleItems.Count) return;
        var item = visibleItems[index];
        if (itemSelected == null)
        {
            var story = item.InspectionDialog;
            if (story == null || DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog) return;
            if (item.InspectionResult != null && !inventory.TryExchange(item.Id, item.InspectionResult.Id)) return;
            Resume();
            DialogManager.Instance.ShowDialog(story);
            return;
        }
        var callback = itemSelected;
        ClearItemSelection();
        Resume();
        callback(item);
    }

    private void ReturnFromItems()
    {
        if (itemSelected == null) { Resume(); return; }
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

    public void ShowTutorialComplete()
    {
        if (IsBlockingGameplay) return;
        completionSavePending = true;
        Pause(); ShowPage(Page.Save);
        Notice("튜토리얼이 끝났습니다.\n진행 상황을 저장한 뒤 메인메뉴로 돌아갑니다.");
    }

    private void ReturnFromSave()
    {
        if (!completionSavePending) { ShowPage(Page.Menu); return; }
        Confirm("저장하지 않고 메인메뉴로 돌아갈까요?",
            () => StartCoroutine(ChangeSession(configuration.titleScene, null)));
    }

    private void HandleManualSaveSuccess(int slot)
    {
        if (completionSavePending)
        {
            completionSavePending = false;
            StartCoroutine(ChangeSession(configuration.titleScene, null));
        }
        else Notice("슬롯 " + slot + "에 저장했습니다.");
    }
}
