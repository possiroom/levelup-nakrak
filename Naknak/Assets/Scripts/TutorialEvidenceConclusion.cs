using System.Linq;
using UnityEngine;

/// <summary>Starts the full A/B reconstruction after all six independent actions are observed.</summary>
public sealed class TutorialEvidenceConclusion : MonoBehaviour
{
    [SerializeField] private SceneFlowContainer flow;
    [SerializeField] private string[] connectionIds = new string[0];
    [SerializeField] private ConditionID[] resetOnStart = new ConditionID[0];
    private bool starting;

    private void Update()
    {
        if (starting || flow == null || !SceneLoadManager.IsMemoryRevisit ||
            SceneLoadManager.CurrentScenePath != gameObject.scene.path || SceneLoadManager.IsCutscene ||
            SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession || GameMenuController.IsBlockingGameplay ||
            DialogManager.Instance == null || DialogManager.Instance.IsDialogOpen ||
            GameStateManager.Instance == null || GameStateManager.Instance.GameState != GameState.Gameplay) return;
        var connection = flow.connections.FirstOrDefault(c => connectionIds.Contains(c.id) && SceneFlowContainer.Passes(c.conditions));
        if (connection != null)
        {
            starting = SceneLoadManager.Instance.Travel(flow, connection);
            if (starting)
                foreach (var flag in resetOnStart) ConditionManager.Instance.Set(flag, 0);
        }
    }
}
