using UnityEngine;

/// <summary>Shared four-direction animation parameters for staged witnesses and evidence replays.</summary>
public static class TutorialActorAnimation
{
    public static Animator Find(Transform actor) => actor != null ? actor.GetComponentInChildren<Animator>(true) : null;

    public static float ReadDirection(Animator animator)
    {
        if (HasParameter(animator, "direction", AnimatorControllerParameterType.Float))
            return animator.GetFloat("direction");
        return (float)Direction.Down;
    }

    public static void SetMotion(Animator animator, Vector2 facing, bool moving)
    {
        if (HasParameter(animator, "isMoving", AnimatorControllerParameterType.Bool))
            animator.SetBool("isMoving", moving);
        // Zero preserves the current facing when an interrupted walk is stopped.
        if (facing.sqrMagnitude > 0.0001f && HasParameter(animator, "direction", AnimatorControllerParameterType.Float))
            animator.SetFloat("direction", (float)(Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ?
                (facing.x > 0 ? Direction.Right : Direction.Left) : (facing.y > 0 ? Direction.Up : Direction.Down)));
    }

    public static void RestoreDirection(Animator animator, float direction)
    {
        SetMotion(animator, Vector2.zero, false);
        if (HasParameter(animator, "direction", AnimatorControllerParameterType.Float))
            animator.SetFloat("direction", direction);
    }

    private static bool HasParameter(Animator animator, string name, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var parameter in animator.parameters)
            if (parameter.name == name && parameter.type == type) return true;
        return false;
    }
}
