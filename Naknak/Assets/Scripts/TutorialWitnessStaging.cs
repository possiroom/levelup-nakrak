using System.Collections;
using UnityEngine;

/// <summary>Witnesses have stable investigation positions; the first visit retains its original timing.</summary>
public sealed class TutorialWitnessStaging : MonoBehaviour
{
    [SerializeField] private Transform witnessA;
    [SerializeField] private Transform witnessB;
    [SerializeField] private bool walkAfterAppraisal;
    [SerializeField] private StoryData appraisalLastLine;
    [SerializeField] private bool facePlayerUpOnEntry;
    [SerializeField] private Vector2 phonePosition = new Vector2(-4, 0);
    [SerializeField] private Vector2 trashPosition = new Vector2(4, -3);
    [SerializeField] private Vector2 cabinetPosition = new Vector2(4, 4);
    [Header("Witness Facing After Arrival")]
    [Tooltip("Up (0,1), right (1,0), down (0,-1), left (-1,0).")]
    [SerializeField] private Vector2 phoneFacing = Vector2.left;
    [SerializeField] private Vector2 trashFacing = Vector2.down;
    [SerializeField] private Vector2 cabinetFacing = Vector2.up;
    private Animator witnessAAnimator;
    private Animator witnessBAnimator;
    private DialogManager watchedManager;
    private bool appraisalFinished;
    public bool AppraisalWalkComplete { get; private set; }
    public bool WaitBeforeTransition(StoryData story) => walkAfterAppraisal && !SceneLoadManager.IsMemoryRevisit &&
        story == appraisalLastLine && witnessA != null && !AppraisalWalkComplete;

    private IEnumerator Start()
    {
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsRestoringSession)
            yield return null;
        witnessAAnimator = TutorialActorAnimation.Find(witnessA);
        witnessBAnimator = TutorialActorAnimation.Find(witnessB);
        if (facePlayerUpOnEntry)
        {
            var player = FindFirstObjectByType<PlayerMoveController3>();
            if (player != null) player.SetFacingDirection(Vector2.up);
        }
        if (SceneLoadManager.IsMemoryRevisit)
        {
            Place(witnessA, phonePosition, phoneFacing, witnessAAnimator);
            Place(witnessB, cabinetPosition, cabinetFacing, witnessBAnimator);
            yield break;
        }
        Place(witnessB, trashPosition, trashFacing, witnessBAnimator);
        if (!walkAfterAppraisal)
        {
            TutorialActorAnimation.SetMotion(witnessAAnimator, phoneFacing, false);
            yield break;
        }
        while (DialogManager.Instance == null) yield return null;
        watchedManager = DialogManager.Instance;
        watchedManager.DialogueFinished += OnDialogueFinished;
        while (!appraisalFinished) yield return null;
        if (witnessA == null) yield break;
        if (!TutorialWalkingRoute.TryBuild(witnessA, witnessA.position, phonePosition, out var route))
        {
            Debug.LogError("[Tutorial route] A cannot reach the telephone safely.", this);
            yield break;
        }
        foreach (var destination in route)
            while ((witnessA.position - destination).sqrMagnitude > 0.001f)
            {
                TutorialActorAnimation.SetMotion(witnessAAnimator, destination - witnessA.position, true);
                witnessA.position = Vector3.MoveTowards(witnessA.position, destination, 2.5f * Time.deltaTime);
                yield return null;
            }
        TutorialActorAnimation.SetMotion(witnessAAnimator, phoneFacing, false);
        AppraisalWalkComplete = true;
    }

    private void OnDialogueFinished(StoryData story)
    {
        if (story == appraisalLastLine) appraisalFinished = true;
    }

    private void OnDisable()
    {
        if (watchedManager != null) watchedManager.DialogueFinished -= OnDialogueFinished;
        TutorialActorAnimation.SetMotion(witnessAAnimator, Vector2.zero, false);
        TutorialActorAnimation.SetMotion(witnessBAnimator, Vector2.zero, false);
    }

    private static void Place(Transform actor, Vector2 position, Vector2 facing, Animator animator)
    {
        if (actor != null) actor.position = new Vector3(position.x, position.y, actor.position.z);
        TutorialActorAnimation.SetMotion(animator, facing, false);
    }
}
