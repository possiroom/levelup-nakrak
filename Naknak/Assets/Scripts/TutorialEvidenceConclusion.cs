using System.Linq;
using UnityEngine;

/// <summary>Announces each completed route, then offers to leave after both alibis are verified.</summary>
public sealed class TutorialEvidenceConclusion : MonoBehaviour
{
    [System.Serializable]
    public sealed class Witness
    {
        public ConditionID[] observed = new ConditionID[0];
        public ConditionID completed;
        public StoryData completionDialog;

        public bool Ready => observed.Length > 0 && observed.All(flag => flag != null && ConditionManager.Instance.Get(flag) != 0);
    }

    [SerializeField] private Witness[] witnesses = new Witness[0];
    [SerializeField] private ConditionID bothCompleted;
    [SerializeField] private StoryData bothCompletedDialog;

    private void Update()
    {
        if (ConditionManager.Instance == null || witnesses.Length == 0 || !SceneLoadManager.IsMemoryRevisit ||
            SceneLoadManager.CurrentScenePath != gameObject.scene.path || SceneLoadManager.IsCutscene ||
            SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession || GameMenuController.IsBlockingGameplay ||
            DialogManager.Instance == null || DialogManager.Instance.IsDialogOpen ||
            GameStateManager.Instance == null || GameStateManager.Instance.GameState != GameState.Gameplay) return;
        foreach (var witness in witnesses)
        {
            if (witness == null || !witness.Ready || witness.completed == null || witness.completionDialog == null ||
                ConditionManager.Instance.Get(witness.completed) != 0) continue;
            DialogManager.Instance.ShowDialog(witness.completionDialog);
            return;
        }
        if (bothCompleted != null && bothCompletedDialog != null && ConditionManager.Instance.Get(bothCompleted) == 0 &&
            witnesses.All(witness => witness != null && witness.Ready && witness.completed != null &&
                ConditionManager.Instance.Get(witness.completed) != 0))
            DialogManager.Instance.ShowDialog(bothCompletedDialog);
    }
}
