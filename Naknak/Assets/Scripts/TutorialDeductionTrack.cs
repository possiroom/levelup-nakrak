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
        [Tooltip("Facing after arrival: up (0,1), right (1,0), down (0,-1), left (-1,0). Walking faces the route automatically.")]
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
        animator = TutorialActorAnimation.Find(actor);
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
        track.animator = TutorialActorAnimation.Find(target);
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
                    Vector2 from = actor.position;
                    if (beat.waypoints != null)
                        foreach (var waypoint in beat.waypoints)
                            if (waypoint != null) QueueWalk(ref from, waypoint.position);
                    Vector2 target = beat.point != null ? (Vector2)beat.point.position : beat.position;
                    QueueWalk(ref from, target);
                    standingFacing = beat.facing;
                    destination = path.Count > 0 ? path.Dequeue() : actor.position;
                }
                break;
            }
        while ((destination - actor.position).sqrMagnitude <= 0.001f && path.Count > 0)
            destination = path.Dequeue();
        var delta = destination - actor.position;
        bool moving = delta.sqrMagnitude > 0.001f;
        TutorialActorAnimation.SetMotion(animator, moving ? (Vector2)delta : standingFacing, moving);
        actor.position = Vector3.MoveTowards(actor.position, destination, walkingSpeed * Time.deltaTime);
    }

    private void QueueWalk(ref Vector2 from, Vector2 target)
    {
        if (!TutorialWalkingRoute.TryBuild(actor, from, target, out var route))
        {
            Debug.LogWarning($"[Tutorial route] No safe route for {actor.name}: {from} -> {target}", this);
            return;
        }
        foreach (var step in route) path.Enqueue(step);
        if (route.Length > 0) from = route[route.Length - 1];
    }
    private void OnDisable()
    {
        TutorialActorAnimation.SetMotion(animator, Vector2.zero, false);
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
