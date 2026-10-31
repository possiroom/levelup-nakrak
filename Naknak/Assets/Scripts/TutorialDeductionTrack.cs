using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>Inspectable dialogue beats reconstruct the witness's route with the existing actor and shop.</summary>
public sealed class TutorialDeductionTrack : MonoBehaviour
{
    [Serializable] public sealed class Beat
    {
        public StoryData dialogue;
        public Vector2 position;
        [Tooltip("Optional scene marker. When assigned, this replaces the numeric destination.")]
        public Transform point;
        [Tooltip("Walk through these scene markers in order before reaching the destination.")]
        public Transform[] waypoints = Array.Empty<Transform>();
        public Vector2 facing = Vector2.up;
    }
    [SerializeField] private Transform actor;
    [SerializeField] private Beat[] beats = Array.Empty<Beat>();
    [SerializeField] private float walkingSpeed = 4;
    private CinemachineCamera trackingCamera;
    private Transform previousFollow;
    private Vector3 destination;
    private Animator animator;
    private readonly System.Collections.Generic.Queue<Vector3> path = new System.Collections.Generic.Queue<Vector3>();
    private StoryData activeBeat;
    private Vector2 standingFacing = Vector2.up;
    private bool ready;
    private static TutorialDeductionTrack activeTrack;

    // Keep the current reconstruction beat visible until its actor reaches the indicated location.
    public static bool IsMoving => activeTrack != null && activeTrack.BlocksAdvance();

    private bool BlocksAdvance()
    {
        if (actor == null) return false;
        if (!ready) return true;
        if (path.Count > 0 || actor != null && (destination - actor.position).sqrMagnitude > 0.001f) return true;
        if (DialogManager.Instance == null) return false;
        foreach (var beat in beats)
            if (beat.dialogue != null && beat.dialogue == DialogManager.Instance.CurrentStory && beat.dialogue != activeBeat) return true;
        return false;
    }

    private void OnEnable() { activeTrack = this; }

    private IEnumerator Start()
    {
        if (ready) yield break; // An item-specific reconstruction has already supplied its track.
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsRestoringSession) yield return null;
        if (actor == null) yield break;
        animator = actor.GetComponent<Animator>();
        if (beats.Length == 0) standingFacing = Vector2.down;
        destination = actor.position;
        trackingCamera = FindFirstObjectByType<CinemachineCamera>();
        if (trackingCamera != null) { previousFollow = trackingCamera.Follow; trackingCamera.Follow = actor; }
        ready = true;
    }

    public static TutorialDeductionTrack BeginSegment(Transform target, Beat[] segment)
    {
        var owner = new GameObject("Item Evidence Reconstruction");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, target.gameObject.scene);
        var track = owner.AddComponent<TutorialDeductionTrack>();
        track.actor = target;
        track.beats = segment;
        track.animator = target.GetComponent<Animator>();
        track.destination = target.position;
        track.trackingCamera = FindFirstObjectByType<CinemachineCamera>();
        if (track.trackingCamera != null)
        {
            track.previousFollow = track.trackingCamera.Follow;
            track.trackingCamera.Follow = target;
        }
        track.ready = true;
        return track;
    }
    private void Update()
    {
        if (!ready || actor == null || SceneLoadManager.IsLoading || DialogManager.Instance == null) return;
        foreach (var beat in beats)
            if (beat.dialogue != null && beat.dialogue == DialogManager.Instance.CurrentStory)
            {
                if (activeBeat != beat.dialogue)
                {
                    activeBeat = beat.dialogue;
                    path.Clear();
                    if (beat.waypoints != null)
                        foreach (var waypoint in beat.waypoints)
                            if (waypoint != null) path.Enqueue(new Vector3(waypoint.position.x, waypoint.position.y, actor.position.z));
                    Vector2 target = beat.point != null ? (Vector2)beat.point.position : beat.position;
                    path.Enqueue(new Vector3(target.x, target.y, actor.position.z));
                    standingFacing = beat.facing;
                    destination = path.Dequeue();
                }
                break;
            }
        while ((destination - actor.position).sqrMagnitude <= 0.001f && path.Count > 0)
            destination = path.Dequeue();
        var delta = destination - actor.position;
        if (animator != null)
        {
            animator.SetBool("isMoving", delta.sqrMagnitude > 0.001f);
            if (delta.sqrMagnitude > 0.001f) animator.SetFloat("direction", (float)(Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ?
                (delta.x > 0 ? Direction.Right : Direction.Left) : (delta.y > 0 ? Direction.Up : Direction.Down)));
            else animator.SetFloat("direction", (float)(Mathf.Abs(standingFacing.x) > Mathf.Abs(standingFacing.y) ?
                (standingFacing.x > 0 ? Direction.Right : Direction.Left) : (standingFacing.y > 0 ? Direction.Up : Direction.Down)));
        }
        actor.position = Vector3.MoveTowards(actor.position, destination, walkingSpeed * Time.deltaTime);
    }
    private void OnDisable()
    {
        if (activeTrack == this) activeTrack = null;
        if (trackingCamera != null && trackingCamera.Follow == actor) trackingCamera.Follow = previousFollow;
    }

    private void OnDrawGizmosSelected()
    {
        if (actor == null) return;
        Vector3 previous = actor.position;
        Gizmos.color = Color.cyan;
        foreach (var beat in beats)
        {
            if (beat.waypoints != null)
                foreach (var waypoint in beat.waypoints)
                    if (waypoint != null) { Gizmos.DrawLine(previous, waypoint.position); previous = waypoint.position; }
            Vector3 target = beat.point != null ? beat.point.position : new Vector3(beat.position.x, beat.position.y, actor.position.z);
            Gizmos.DrawLine(previous, target); Gizmos.DrawWireSphere(target, 0.15f); previous = target;
        }
    }
}
