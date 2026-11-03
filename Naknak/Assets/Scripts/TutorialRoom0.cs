using System.Collections;
using Naknak.Inventory;
using UnityEngine;

/// <summary>T0's existing room, inventory window, dialogue and scene-flow services.</summary>
public sealed class TutorialRoom0 : MonoBehaviour
{
    [SerializeField] private InventoryItem brooch;
    [SerializeField] private InventoryItem packedBox;
    [SerializeField] private ConditionID initialized;
    [SerializeField] private ConditionID packed;
    [SerializeField] private SpriteRenderer sparkle;
    [SerializeField] private GameObject jewelryBox;
    [SerializeField] private StoryData packedMessage;
    [SerializeField] private StoryData wrongItemMessage;
    [SerializeField] private StoryData exitBlockedMessage;
    [SerializeField] private SceneTransitionPoint exit;
    [SerializeField] private ConditionID broochCollected;
    [SerializeField] private GameObject broochObject;
    [SerializeField] private StoryData broochMessage;
    private bool ready;

    public bool IsPacked => ConditionManager.Instance != null && ConditionManager.Instance.Get(packed) != 0 ||
        GameMenuController.Instance != null && packedBox != null && GameMenuController.Inventory.Has(packedBox.Id);

    private IEnumerator Start()
    {
        while (GameMenuController.Instance == null || ConditionManager.Instance == null ||
               SceneLoadManager.IsRestoringSession || SceneLoadManager.IsPreparingMemory || GameMenuController.IsBlockingGameplay) yield return null;
        if (brooch == null || packedBox == null || initialized == null || packed == null)
        { Debug.LogError("[T0] 아이템과 진행 조건 연결을 확인해주세요.", this); yield break; }
        // Older saves that already own or packed the brooch must not spawn another pickup.
        if (broochCollected != null && (IsPacked || GameMenuController.Inventory.Has(brooch.Id)))
            ConditionManager.Instance.Set(broochCollected, 1);
        if (ConditionManager.Instance.Get(initialized) == 0)
        {
            ConditionManager.Instance.Set(initialized, 1);
        }
        ready = true;
        RefreshBrooch();
        RefreshSparkle();
    }

    private void Update()
    {
        if (!ready || sparkle == null) return;
        RefreshSparkle();
        if (sparkle.enabled)
        {
            var color = sparkle.color;
            color.a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            sparkle.color = color;
        }
    }

    private void RefreshSparkle()
    {
        if (sparkle != null) sparkle.enabled = !IsPacked;
        if (jewelryBox != null) jewelryBox.SetActive(!IsPacked);
    }

    private bool CanInteract => ready && !GameMenuController.IsBlockingGameplay &&
        (DialogManager.Instance == null || !DialogManager.Instance.IsDialogOpen) &&
        (GameStateManager.Instance == null || GameStateManager.Instance.GameState == GameState.Gameplay);

    private void RefreshBrooch()
    {
        if (broochObject != null)
            broochObject.SetActive(!IsPacked && !GameMenuController.Inventory.Has(brooch.Id) &&
                (broochCollected == null || ConditionManager.Instance.Get(broochCollected) == 0));
    }

    public void CollectBrooch()
    {
        if (!CanInteract || IsPacked || GameMenuController.Inventory.Has(brooch.Id) ||
            broochCollected != null && ConditionManager.Instance.Get(broochCollected) != 0) return;
        if (!GameMenuController.Inventory.Add(brooch.Id)) return;
        if (broochCollected != null) ConditionManager.Instance.Set(broochCollected, 1);
        RefreshBrooch();
        Show(broochMessage);
    }

    public void InteractWithBox()
    {
        if (!CanInteract) return;
        if (IsPacked) { Show(packedMessage); return; }
        GameMenuController.Instance.SelectInventoryItem("보석함에 넣을 아이템을 선택하세요.", UseItem);
    }

    private void UseItem(InventoryItem item)
    {
        if (this == null || !isActiveAndEnabled || IsPacked) return;
        if (item == null || item.Id != brooch.Id || !GameMenuController.Inventory.TryExchange(brooch.Id, packedBox.Id))
        { Show(wrongItemMessage); return; }
        ConditionManager.Instance.Set(packed, 1);
        RefreshSparkle();
        Show(packedMessage);
    }

    public void TryLeaveRoom()
    {
        if (!CanInteract) return;
        if (!IsPacked) { Show(exitBlockedMessage); return; }
        if (exit != null) exit.Interact();
    }

    private static void Show(StoryData story)
    { if (story != null && DialogManager.Instance != null) DialogManager.Instance.ShowDialog(story); }
}
