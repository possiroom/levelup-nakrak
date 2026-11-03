using System.Collections.Generic;
using Naknak.Inventory;
using UnityEngine;

/// <summary>Uses existing interaction, story variables, dialogue and inventory/save services.</summary>
public sealed class MemoryInvestigationPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private ConditionID available;
    [SerializeField] private ConditionID inspected;
    [SerializeField] private InventoryItem[] rewards = new InventoryItem[0];
    [SerializeField] private StoryData firstDialog;
    [SerializeField] private StoryData repeatDialog;
    [SerializeField] private SpriteRenderer sparkle;

    private bool IsInspected => ConditionManager.Instance != null && ConditionManager.Instance.Get(inspected) != 0;

    private void Update()
    {
        if (sparkle != null) sparkle.enabled = !SceneLoadManager.IsFirstMemoryVisit && !IsInspected;
    }

    public void Interact()
    {
        if (GameMenuController.Instance == null || ConditionManager.Instance == null || inspected == null ||
            GameMenuController.IsBlockingGameplay || SceneLoadManager.IsFirstMemoryVisit || SceneLoadManager.IsCutscene || DialogManager.Instance == null ||
            DialogManager.Instance.IsDialogOpen || GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay)
            return;
        if (available != null && ConditionManager.Instance.Get(available) == 0) return;
        if (IsInspected)
        {
            if (repeatDialog != null) DialogManager.Instance.ShowDialog(repeatDialog);
            return;
        }
        var items = new List<InventoryEntry>();
        foreach (var item in rewards)
        {
            if (item == null) { Debug.LogError("[Memory] 조사 보상 아이템을 확인해주세요.", this); return; }
            items.Add(new InventoryEntry(item.Id, 1));
        }
        if (!GameMenuController.Inventory.TryAddAll(items))
        { Debug.LogWarning("[Memory] 조사 아이템을 받을 수 없습니다. 소지 수량과 아이템 목록을 확인해주세요.", this); return; }
        ConditionManager.Instance.Set(inspected, 1);
        if (sparkle != null) sparkle.enabled = false;
        if (firstDialog != null) DialogManager.Instance.ShowDialog(firstDialog);
    }
}
