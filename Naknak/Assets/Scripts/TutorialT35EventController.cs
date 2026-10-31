using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialT35EventController : MonoBehaviour, IInteractable
{
    [Header("Player")]
    [Tooltip("비워두면 Player 태그 또는 PlayerMoveController3로 자동 탐색")]
    [SerializeField] private Transform player;

    [Header("Darkness")]
    [SerializeField] private Image darkOverlayImage;
    [SerializeField, Range(0f, 1f)] private float darkAlpha = 0.85f;
    [SerializeField] private float darkFadeInTime = 0.25f;
    [SerializeField] private float darkFadeOutTime = 0.35f;

    [Header("Search Object")]
    [SerializeField] private Transform droppedOpenBoxInteract;
    [SerializeField] private GameObject sparkleIcon;

    [Tooltip("ON이면 플레이어가 가까이 왔을 때만 반짝이 표시. 테스트 중이면 OFF 추천.")]
    [SerializeField] private bool showSparkleOnlyWhenNearPlayer = true;

    [Tooltip("플레이어가 이 거리 안에 들어오면 반짝이 표시")]
    [SerializeField] private float revealDistance = 2.0f;

    [Tooltip("플레이어가 이 거리 안에서 상호작용하면 획득")]
    [SerializeField] private float interactDistance = 1.2f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.Z;

    [Header("Built-in Monologue")]
    [SerializeField] private StoryData boxFoundDialog;
    [SerializeField] private Naknak.Inventory.InventoryItem foundEvidence;
    [SerializeField] private ConditionID boxFound;
    [TextArea(2, 4)]
    [SerializeField] private string getBoxMonologueText = "바닥에 떨어진 열린 상자를 획득했다.";

    [Tooltip("비워두면 자동 생성됨")]
    [SerializeField] private Canvas monologueCanvas;

    [Tooltip("비워두면 자동 생성됨")]
    [SerializeField] private GameObject monologuePanel;

    [Tooltip("비워두면 자동 생성됨")]
    [SerializeField] private TMP_Text monologueText;

    [SerializeField] private float monologueInputLockTime = 0.2f;

    [Header("Result")]
    [SerializeField] private bool fadeOutAfterGetBox = true;
    [SerializeField] private bool loadNextSceneAfterGetBox = false;
    [SerializeField] private string nextSceneName = "Tutorial_T4_BroochMissing";
    [SerializeField] private float waitAfterGetBox = 0.3f;

    private bool isRunning = false;
    private bool obtainedBox = false;
    private bool showingMonologue = false;

    private void Awake()
    {
        AutoFindPlayer();
        if (boxFoundDialog == null) EnsureMonologueUI();
    }

    private IEnumerator Start()
    {
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession ||
               GameMenuController.IsBlockingGameplay ||
               boxFound != null && ConditionManager.Instance == null ||
               boxFoundDialog != null && (DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog))
            yield return null;

        if (boxFound != null && ConditionManager.Instance.Get(boxFound) != 0)
        { SceneLoadManager.Instance?.TryCompleteScene(); yield break; }

        StartT35();
    }

    private void Update()
    {
        if (GameMenuController.IsBlockingGameplay || SceneLoadManager.IsRestoringSession) return;
        if (!isRunning)
            return;

        if (obtainedBox)
            return;

        if (showingMonologue)
            return;

        AutoFindPlayer();
        UpdateSparkleVisibility();

        if (InteractionInput.ConfirmPressed)
        {
            InteractionInput.ConsumeConfirm();
            Interact();
        }
    }

    public void StartT35()
    {
        if (isRunning)
            return;

        StartCoroutine(StartT35Routine());
    }

    private IEnumerator StartT35Routine()
    {
        isRunning = true;
        obtainedBox = false;
        showingMonologue = false;

        AutoFindPlayer();

        if (droppedOpenBoxInteract != null)
            droppedOpenBoxInteract.gameObject.SetActive(true);

        UpdateSparkleVisibility();

        yield return FadeDarkOverlay(darkAlpha, darkFadeInTime);

        Debug.Log("[TutorialT35EventController] T3.5 시작");
    }

    private void UpdateSparkleVisibility()
    {
        if (sparkleIcon == null)
            return;

        if (!showSparkleOnlyWhenNearPlayer)
        {
            sparkleIcon.SetActive(true);
            return;
        }

        if (player == null || droppedOpenBoxInteract == null)
        {
            sparkleIcon.SetActive(false);
            return;
        }

        float distance = Vector2.Distance(player.position, droppedOpenBoxInteract.position);

        sparkleIcon.SetActive(distance <= revealDistance);
    }

    public void Interact()
    {
        if (GameMenuController.IsBlockingGameplay || SceneLoadManager.IsRestoringSession ||
            GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;
        AutoFindPlayer();
        TryGetDroppedBox();
    }

    private void TryGetDroppedBox()
    {
        if (!isRunning || obtainedBox || showingMonologue) return;
        if (player == null || droppedOpenBoxInteract == null)
            return;

        float distance = Vector2.Distance(player.position, droppedOpenBoxInteract.position);

        if (distance > interactDistance)
            return;

        StartCoroutine(GetDroppedBoxRoutine());
    }

    private IEnumerator GetDroppedBoxRoutine()
    {
        if (foundEvidence != null && (GameMenuController.Instance == null ||
            !GameMenuController.Inventory.Has(foundEvidence.Id) && !GameMenuController.Inventory.Add(foundEvidence.Id)))
            yield break;
        obtainedBox = true;

        if (sparkleIcon != null)
            sparkleIcon.SetActive(false);

        Debug.Log("[TutorialT35EventController] 열린 상자 상호작용");

        yield return ShowMonologueRoutine(getBoxMonologueText);

        if (boxFound != null) ConditionManager.Instance.Set(boxFound, 1);

        if (droppedOpenBoxInteract != null)
            droppedOpenBoxInteract.gameObject.SetActive(false);

        Debug.Log("[TutorialT35EventController] 바닥에 떨어진 열린 상자를 획득했습니다.");

        yield return new WaitForSeconds(waitAfterGetBox);

        if (fadeOutAfterGetBox)
            yield return FadeDarkOverlay(0f, darkFadeOutTime);

        isRunning = false;

        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.TryCompleteScene()) yield break;

        if (loadNextSceneAfterGetBox && !string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneLoadManager.Instance != null) yield return SceneLoadManager.Instance.LoadContent(nextSceneName);
            else SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("[TutorialT35EventController] T3.5 종료. 다음 단계 진행 가능.");
        }
    }

    private IEnumerator ShowMonologueRoutine(string text)
    {
        if (boxFoundDialog != null && DialogManager.Instance != null && DialogManager.Instance.CanShowDialog)
        {
            showingMonologue = true;
            DialogManager.Instance.ShowDialog(boxFoundDialog);
            yield return null;
            while (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
            showingMonologue = false;
            yield break;
        }
        EnsureMonologueUI();

        if (monologuePanel == null || monologueText == null)
        {
            Debug.LogWarning("[TutorialT35EventController] 모노로그 UI가 없어 로그로만 출력: " + text);
            yield return new WaitForSeconds(1f);
            yield break;
        }

        showingMonologue = true;

        monologueText.text = text;
        monologuePanel.SetActive(true);

        // 상호작용에 사용한 Z 입력이 바로 모노로그를 닫아버리는 것 방지
        yield return new WaitForSeconds(monologueInputLockTime);

        while (!GetAdvanceInput())
            yield return null;

        monologuePanel.SetActive(false);
        showingMonologue = false;
    }

    private bool GetAdvanceInput()
    {
        if (Input.GetKeyDown(interactKey))
            return true;

        if (Input.GetKeyDown(KeyCode.Return))
            return true;

        if (Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;

        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        if (Input.GetMouseButtonDown(0))
            return true;

        return false;
    }

    private void EnsureMonologueUI()
    {
        if (monologuePanel != null && monologueText != null)
        {
            monologuePanel.SetActive(false);
            return;
        }

        if (monologueCanvas == null)
        {
            GameObject canvasObj = new GameObject("T35_MonologueCanvas");
            monologueCanvas = canvasObj.AddComponent<Canvas>();
            monologueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            monologueCanvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (monologuePanel == null)
        {
            GameObject panelObj = new GameObject("T35_MonologuePanel");
            panelObj.transform.SetParent(monologueCanvas.transform, false);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);
            panelImage.raycastTarget = false;

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.05f);
            panelRect.anchorMax = new Vector2(0.92f, 0.22f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            monologuePanel = panelObj;
        }

        if (monologueText == null)
        {
            GameObject textObj = new GameObject("T35_MonologueText");
            textObj.transform.SetParent(monologuePanel.transform, false);

            monologueText = textObj.AddComponent<TextMeshProUGUI>();
            monologueText.text = "";
            monologueText.fontSize = 36f;
            monologueText.alignment = TextAlignmentOptions.MidlineLeft;
            monologueText.color = Color.white;
            monologueText.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(40f, 20f);
            textRect.offsetMax = new Vector2(-40f, -20f);
        }

        monologuePanel.SetActive(false);
    }

    private IEnumerator FadeDarkOverlay(float targetAlpha, float duration)
    {
        if (darkOverlayImage == null)
            yield break;

        float startAlpha = darkOverlayImage.color.a;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetDarkOverlayAlpha(targetAlpha);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetDarkOverlayAlpha(alpha);

            yield return null;
        }

        SetDarkOverlayAlpha(targetAlpha);
    }

    private void SetDarkOverlayAlpha(float alpha)
    {
        if (darkOverlayImage == null)
            return;

        Color color = darkOverlayImage.color;
        color.a = alpha;
        darkOverlayImage.color = color;
    }

    private void AutoFindPlayer()
    {
        if (player != null)
            return;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");

        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return;
        }

        PlayerMoveController3 moveController = FindFirstObjectByType<PlayerMoveController3>();

        if (moveController != null)
            player = moveController.transform;
    }
}
