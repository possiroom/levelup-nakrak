using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>S1: dispute, detective entrance, investigation announcement.</summary>
public sealed class TutorialScene1Cutscene : MonoBehaviour
{
    public enum CutscenePhase { WaitingForServices, Opening, Dispute, Entrance, Walking, Investigation, Finished }
    [Header("대화 컨테이너")]
    [SerializeField] private DialogContainer disputeDialog;
    [SerializeField] private DialogContainer entranceDialog;
    [SerializeField] private DialogContainer investigationDialog;

    [Header("탐정과 이동 포인트")]
    [SerializeField] private Transform detective;
    [SerializeField] private Transform detectiveStartPoint;
    [SerializeField] private Transform[] detectiveWaypoints = new Transform[0];
    [SerializeField] private Transform detectiveCenterPoint;
    [SerializeField, Min(0.1f)] private float walkSpeed = 2.5f;
    [SerializeField] private Vector2 detectiveFinalFacing = Vector2.left;

    [Header("카메라 포인트")]
    [SerializeField] private Camera cutsceneCamera;
    [Tooltip("S1 카메라를 자동으로 움직이는 기존 Cinemachine Brain 등")]
    [SerializeField] private Behaviour cameraDriver;
    [SerializeField] private Transform openingCameraPoint;
    [SerializeField] private Transform entranceCameraPoint;
    [SerializeField] private Transform groupCameraPoint;
    [SerializeField, Min(0.1f)] private float openingCameraSize = 2.6f;
    [SerializeField, Min(0.1f)] private float groupCameraSize = 3.8f;
    [SerializeField, Min(0f)] private float cameraMoveTime = 0.7f;
    [SerializeField] private bool followDetectiveDuringWalk = true;

    [Header("페이드인")]
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] private float fadeInTime = 0.45f;
    [SerializeField] private CutscenePhase phase;
    public CutscenePhase Phase => phase;

    private bool waitingForDialog;
    private bool completionSent;
    private Animator detectiveAnimator;

    private void Awake()
    {
        phase = CutscenePhase.WaitingForServices;
        if (cameraDriver != null) cameraDriver.enabled = false;
        SetFade(1f);
        if (detective != null)
        {
            if (detectiveStartPoint != null) detective.position = detectiveStartPoint.position;
            detectiveAnimator = detective.GetComponentInChildren<Animator>(true);
            detective.gameObject.SetActive(true);
            FocusDetective();
        }
    }

    private IEnumerator Start()
    {
        // The menu resets gameplay state after loading. Do not open dialogue before it finishes.
        while (GameMenuController.IsBlockingGameplay) yield return null;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog || ConditionManager.Instance == null)
        {
            if (Time.realtimeSinceStartup >= deadline)
            { Debug.LogError("[S1] 공통 대화 시스템을 불러오지 못했습니다.", this); yield break; }
            yield return null;
        }
        if (!ValidateReferences()) yield break;
        var dispute = disputeDialog.GetMatchingDialog();
        var entrance = entranceDialog.GetMatchingDialog();
        var investigation = investigationDialog.GetMatchingDialog();
        if (dispute == null || entrance == null || investigation == null)
        { Debug.LogError("[S1] 대화 컨테이너의 시작 대사/조건을 확인해주세요.", this); yield break; }

        DialogManager.Instance.BindSceneViews(gameObject.scene);
        phase = CutscenePhase.Opening;
        yield return FadeIn();
        phase = CutscenePhase.Dispute;
        yield return PlayStory(dispute);

        phase = CutscenePhase.Entrance;
        detective.gameObject.SetActive(true);
        SetWalking(false, Vector2.up);

        phase = CutscenePhase.Walking;
        // Dialogue is closed during forced movement; input cannot move the real player.
        if (detectiveWaypoints != null)
            foreach (var point in detectiveWaypoints)
                if (point != null) yield return WalkTo(point.position);
        yield return WalkTo(detectiveCenterPoint.position);
        SetWalking(false, detectiveFinalFacing);
        FocusDetective();
        yield return PlayStory(entrance);

        phase = CutscenePhase.Investigation;
        yield return PlayStory(investigation);
        phase = CutscenePhase.Finished;
        SetFade(0f); // S2 continues the same conversation; memory-entry fade belongs to S2.
        if (!completionSent)
        {
            completionSent = true;
            if (SceneLoadManager.Instance == null || !SceneLoadManager.Instance.TryCompleteScene())
                Debug.LogWarning("[S1] Scene Flow Container에서 S1 완료 후 연결을 확인해주세요.", this);
        }
    }

    private bool ValidateReferences()
    {
        if (disputeDialog != null && entranceDialog != null && investigationDialog != null &&
            detective != null && detectiveStartPoint != null && detectiveCenterPoint != null &&
            cutsceneCamera != null && openingCameraPoint != null && entranceCameraPoint != null && groupCameraPoint != null)
            return true;
        Debug.LogError("[S1] CutsceneManager에 대화, 탐정, 이동/카메라 포인트를 지정해주세요.", this);
        return false;
    }

    private IEnumerator PlayStory(StoryData story)
    {
        waitingForDialog = true;
        DialogManager.Instance.ShowDialog(story, true);
        while (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
        waitingForDialog = false;
        // The confirm key that closed this conversation cannot close the next one.
        yield return null;
    }

    private void Update()
    {
        // Managed scenes have exactly one dialogue input owner: SceneLoadManager.
        if (!waitingForDialog || SceneLoadManager.HasManagedScene || GameMenuController.IsBlockingGameplay) return;
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) DialogManager.Instance?.AdvanceFromInput();
    }

    private void LateUpdate() => FocusDetective();

    private void FocusDetective()
    {
        if (cutsceneCamera == null || detective == null) return;
        cutsceneCamera.transform.position = new Vector3(detective.position.x, detective.position.y + 1f,
            cutsceneCamera.transform.position.z);
        cutsceneCamera.orthographicSize = groupCameraSize;
    }

    private IEnumerator WalkTo(Vector3 target)
    {
        while (Vector3.Distance(detective.position, target) > 0.001f)
        {
            SetWalking(true, target - detective.position);
            detective.position = Vector3.MoveTowards(detective.position, target, walkSpeed * Time.deltaTime);
            if (followDetectiveDuringWalk && cutsceneCamera != null)
            {
                var focus = new Vector3(detective.position.x, detective.position.y + 1f, cutsceneCamera.transform.position.z);
                cutsceneCamera.transform.position = Vector3.Lerp(cutsceneCamera.transform.position, focus, 4f * Time.deltaTime);
            }
            yield return null;
        }
        detective.position = target;
        SetWalking(false, Vector2.up);
    }

    private void SetWalking(bool moving, Vector2 direction)
    {
        if (detectiveAnimator == null || detectiveAnimator.runtimeAnimatorController == null) return;
        detectiveAnimator.SetBool("isMoving", moving);
        float facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? (direction.x > 0 ? 1 : 3) : (direction.y > 0 ? 0 : 2);
        detectiveAnimator.SetFloat("direction", facing);
    }

    private void SetShot(Transform point, float size)
    {
        if (cutsceneCamera == null || point == null) return;
        cutsceneCamera.transform.position = point.position;
        cutsceneCamera.orthographicSize = size;
    }

    private IEnumerator MoveCamera(Transform point, float size)
    {
        Vector3 from = cutsceneCamera.transform.position;
        float fromSize = cutsceneCamera.orthographicSize;
        float elapsed = 0f;
        while (elapsed < cameraMoveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraMoveTime);
            cutsceneCamera.transform.position = Vector3.Lerp(from, point.position, t);
            cutsceneCamera.orthographicSize = Mathf.Lerp(fromSize, size, t);
            yield return null;
        }
        SetShot(point, size);
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            SetFade(1f - Mathf.Clamp01(elapsed / fadeInTime));
            yield return null;
        }
        SetFade(0f);
    }

    private void SetFade(float alpha)
    {
        if (fadeImage == null) return;
        fadeImage.raycastTarget = false;
        fadeImage.color = new Color(0f, 0f, 0f, alpha);
    }

    private void OnDrawGizmosSelected()
    {
        if (detectiveStartPoint == null) return;
        Gizmos.color = Color.cyan;
        Vector3 previous = detectiveStartPoint.position;
        Gizmos.DrawWireSphere(previous, 0.15f);
        if (detectiveWaypoints != null)
            foreach (var point in detectiveWaypoints)
                if (point != null) { Gizmos.DrawLine(previous, point.position); previous = point.position; Gizmos.DrawWireSphere(previous, 0.15f); }
        if (detectiveCenterPoint != null)
        { Gizmos.DrawLine(previous, detectiveCenterPoint.position); Gizmos.DrawWireSphere(detectiveCenterPoint.position, 0.2f); }
    }
}
