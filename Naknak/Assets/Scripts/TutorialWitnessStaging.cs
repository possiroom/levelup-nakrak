using System.Collections;
using UnityEngine;

/// <summary>Witnesses have stable investigation positions; the first visit retains its original timing.</summary>
public sealed class TutorialWitnessStaging : MonoBehaviour
{
    [SerializeField] private Transform witnessA;
    [SerializeField] private Transform witnessB;
    [SerializeField] private bool walkAfterAppraisal;
    [SerializeField] private StoryData appraisalLastLine;
    [SerializeField] private Vector2 phonePosition = new Vector2(-4, 0);
    [SerializeField] private Vector2 trashPosition = new Vector2(4, -3);
    [SerializeField] private Vector2 cabinetPosition = new Vector2(5, 4);
    private DialogManager watchedManager;
    private bool appraisalFinished;
    public bool AppraisalWalkComplete { get; private set; }
    public bool WaitBeforeTransition(StoryData story) => walkAfterAppraisal && !SceneLoadManager.IsMemoryRevisit &&
        story == appraisalLastLine && witnessA != null && !AppraisalWalkComplete;

    private IEnumerator Start()
    {
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsRestoringSession)
            yield return null;
        if (SceneLoadManager.IsMemoryRevisit)
        {
            Place(witnessA, phonePosition);
            Place(witnessB, cabinetPosition);
            yield break;
        }
        Place(witnessB, trashPosition);
        if (!walkAfterAppraisal) yield break;
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
                witnessA.position = Vector3.MoveTowards(witnessA.position, destination, 2.5f * Time.deltaTime);
                yield return null;
            }
        AppraisalWalkComplete = true;
    }

    private void OnDialogueFinished(StoryData story)
    {
        if (story == appraisalLastLine) appraisalFinished = true;
    }

    private void OnDisable()
    {
        if (watchedManager != null) watchedManager.DialogueFinished -= OnDialogueFinished;
    }

    private static void Place(Transform actor, Vector2 position)
    {
        if (actor != null) actor.position = new Vector3(position.x, position.y, actor.position.z);
    }
}
