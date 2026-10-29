using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A small objective display driven by scene/condition data, with the existing menu for completion.</summary>
public sealed class TutorialObjectiveController : MonoBehaviour
{
    private TutorialCaseDefinition definition;
    private TextMeshProUGUI label;
    private GameObject panel;
    private bool completionShown;
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TutorialObjectiveController>() != null) return;
        var definition = Resources.Load<TutorialCaseDefinition>("TutorialCase");
        if (definition == null) return;
        var owner = new GameObject("Tutorial Objective").AddComponent<TutorialObjectiveController>();
        owner.definition = definition;
        DontDestroyOnLoad(owner.gameObject);
        owner.CreateDisplay();
    }

    private void CreateDisplay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        panel = new GameObject("Current Objective", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(18, -18); rect.sizeDelta = new Vector2(640, 64);
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        panel.GetComponent<Image>().raycastTarget = false;
        var textObject = new GameObject("Objective Text", typeof(RectTransform));
        textObject.transform.SetParent(panel.transform, false);
        label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = definition.font;
        label.fontSize = 23; label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 23;
        label.raycastTarget = false;
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14, 8); textRect.offsetMax = new Vector2(-14, -8);
        panel.SetActive(false);
    }

    private void Update()
    {
        if (definition == null || panel == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.15f;
        var conditions = ConditionManager.Instance;
        bool solved = conditions != null && conditions.Get(definition.solved) != 0;
        if (!solved) completionShown = false;
        bool gameplay = SceneLoadManager.HasManagedScene && !SceneLoadManager.IsLoading &&
            !SceneLoadManager.IsRestoringSession && !GameMenuController.IsBlockingGameplay &&
            !SceneLoadManager.IsCutscene && GameStateManager.Instance != null &&
            GameStateManager.Instance.GameState == GameState.Gameplay;
        string text = null;
        if (gameplay)
            foreach (var objective in definition.objectives)
            {
                if (!string.IsNullOrEmpty(objective.scene.Path) && objective.scene.Path != SceneLoadManager.CurrentScenePath) continue;
                if (!SceneFlowContainer.Passes(objective.conditions)) continue;
                text = objective.text; break;
            }
        label.text = text ?? ""; panel.SetActive(!string.IsNullOrEmpty(text));
        if (solved && gameplay && !completionShown && GameMenuController.Instance != null)
        { completionShown = true; GameMenuController.Instance.ShowTutorialComplete(); }
    }
}
