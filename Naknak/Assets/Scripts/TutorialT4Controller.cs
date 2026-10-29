using System.Collections;
using Naknak.Inventory;
using UnityEngine;

/// <summary>Return the box, discover the missing brooch, then freely explore the memory.</summary>
public sealed class TutorialT4Controller : MonoBehaviour
{
    [SerializeField] private DialogContainer returnDialog;
    [SerializeField] private DialogContainer missingDialog;
    [SerializeField] private InventoryItem packedBox;
    [SerializeField] private InventoryItem emptyBox;
    [SerializeField] private ConditionID returned;
    [SerializeField] private ConditionID completed;
    [SerializeField] private GameObject brokenVase;
    [SerializeField] private GameObject investigationObjects;
    public enum T4Phase { WaitingForServices, ReturnDialogue, MissingDialogue, Exploration }
    public T4Phase Phase { get; private set; }

    private IEnumerator Start()
    {
        if (brokenVase != null) brokenVase.SetActive(true);
        if (investigationObjects != null) investigationObjects.SetActive(false);
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession || SceneLoadManager.IsPreparingMemory || GameMenuController.IsBlockingGameplay ||
               GameMenuController.Instance == null || ConditionManager.Instance == null ||
               DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog || DialogManager.Instance.IsDialogOpen)
            yield return null;
        if (returned == null || completed == null || packedBox == null || emptyBox == null)
        { Debug.LogError("[T4] 보석함 아이템과 진행 조건을 연결해주세요.", this); yield break; }
        if (ConditionManager.Instance.Get(completed) != 0) { BeginExploration(); yield break; }
        DialogManager.Instance.BindSceneViews(gameObject.scene);
        SceneLoadManager.Instance?.SetPlayerInputLocked(true);
        if (ConditionManager.Instance.Get(returned) == 0)
        {
            var story = returnDialog != null ? returnDialog.GetMatchingDialog() : null;
            if (story == null) { Debug.LogError("[T4] 반환 대화를 연결해주세요.", this); yield break; }
            Phase = T4Phase.ReturnDialogue;
            yield return Play(story);
            if (!ReturnEmptyBox()) { Debug.LogError("[T4] 빈 보석함을 지급하지 못했습니다. 아이템 목록을 확인해주세요.", this); yield break; }
        }
        var missing = missingDialog != null ? missingDialog.GetMatchingDialog() : null;
        if (missing == null) { Debug.LogError("[T4] 분실 확인 대화를 연결해주세요.", this); yield break; }
        Phase = T4Phase.MissingDialogue;
        yield return Play(missing);
        ConditionManager.Instance.Set(completed, 1);
        BeginExploration();
    }

    private bool ReturnEmptyBox()
    {
        var inventory = GameMenuController.Inventory;
        bool success;
        if (inventory.Has(packedBox.Id))
            success = inventory.Has(emptyBox.Id) ? inventory.Remove(packedBox.Id) : inventory.TryExchange(packedBox.Id, emptyBox.Id);
        else
            // The NPC can already be holding the submitted box; the return grants the empty one.
            success = inventory.Has(emptyBox.Id) || inventory.Add(emptyBox.Id);
        if (success) ConditionManager.Instance.Set(returned, 1);
        return success;
    }

    private static IEnumerator Play(StoryData story)
    {
        DialogManager.Instance.ShowDialog(story);
        yield return null;
        while (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
    }

    private void BeginExploration()
    {
        Phase = T4Phase.Exploration;
        if (investigationObjects != null) investigationObjects.SetActive(true);
        SceneLoadManager.Instance?.SetPlayerInputLocked(false);
    }
}
