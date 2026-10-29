using Naknak.Inventory;
using UnityEngine;

/// <summary>Pause the existing owner conversation to submit an item using the inventory menu.</summary>
public sealed class TutorialT2Appraisal : MonoBehaviour
{
    [SerializeField] private ConditionID awaitingItem;
    [SerializeField] private ConditionID submitted;
    [SerializeField] private InventoryItem packedBox;
    [SerializeField] private StoryData appraisalStart;
    [SerializeField] private StoryData wrongItemDialog;

    private void Update()
    {
        if (awaitingItem == null || submitted == null || packedBox == null || appraisalStart == null ||
            ConditionManager.Instance == null || ConditionManager.Instance.Get(awaitingItem) == 0 ||
            SceneLoadManager.IsRestoringSession || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsCutscene || GameMenuController.IsBlockingGameplay ||
            GameMenuController.Instance == null || DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog ||
            DialogManager.Instance.IsDialogOpen ||
            GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay)
            return;

        // The existing StoryData end action requests selection after "show me" closes.
        ConditionManager.Instance.Set(awaitingItem, 0);
        if (!GameMenuController.Instance.SelectInventoryItem("감정받을 물건을 선택하세요.", SubmitItem))
            ConditionManager.Instance.Set(awaitingItem, 1);
    }

    private void SubmitItem(InventoryItem item)
    {
        if (this == null || !isActiveAndEnabled || SceneLoadManager.IsLoading || DialogManager.Instance == null) return;
        if (item == null || item.Id != packedBox.Id || !GameMenuController.Inventory.Has(packedBox.Id))
        {
            if (wrongItemDialog != null) DialogManager.Instance.ShowDialog(wrongItemDialog);
            return;
        }
        // The conversation refuses the sale. Keep the item until T4 exchanges it on return.
        ConditionManager.Instance.Set(submitted, 1);
        DialogManager.Instance.ShowDialog(appraisalStart);
    }
}
