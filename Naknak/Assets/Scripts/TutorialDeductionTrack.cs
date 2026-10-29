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
    }
    [SerializeField] private Transform actor;
    [SerializeField] private Beat[] beats = Array.Empty<Beat>();
    [SerializeField] private float walkingSpeed = 4;
    private CinemachineCamera trackingCamera;
    private Transform previousFollow;
    private Vector3 destination;
    private Animator animator;

    private IEnumerator Start()
    {
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsRestoringSession) yield return null;
        if (actor == null) yield break;
        animator = actor.GetComponent<Animator>();
        destination = actor.position;
        trackingCamera = FindFirstObjectByType<CinemachineCamera>();
        if (trackingCamera != null) { previousFollow = trackingCamera.Follow; trackingCamera.Follow = actor; }
    }
    private void Update()
    {
        if (actor == null || SceneLoadManager.IsLoading || DialogManager.Instance == null) return;
        foreach (var beat in beats)
            if (beat.dialogue != null && beat.dialogue == DialogManager.Instance.CurrentStory)
            { destination = new Vector3(beat.position.x, beat.position.y, actor.position.z); break; }
        var delta = destination - actor.position;
        if (animator != null)
        {
            animator.SetBool("isMoving", delta.sqrMagnitude > 0.001f);
            if (delta.sqrMagnitude > 0.001f) animator.SetFloat("direction", (float)(Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ?
                (delta.x > 0 ? Direction.Right : Direction.Left) : (delta.y > 0 ? Direction.Up : Direction.Down)));
        }
        actor.position = Vector3.MoveTowards(actor.position, destination, walkingSpeed * Time.deltaTime);
    }
    private void OnDestroy()
    {
        if (trackingCamera != null && trackingCamera.Follow == actor) trackingCamera.Follow = previousFollow;
    }
}
