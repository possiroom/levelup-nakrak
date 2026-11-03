using UnityEngine;

/// <summary>One confirmation per frame, shared by dialogue, menus and world interaction.</summary>
public static class InteractionInput
{
    private static int handledFrame = -1;
    public static bool ConfirmPressed => handledFrame != Time.frameCount &&
        (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
    public static bool CanConfirm => handledFrame != Time.frameCount;
    public static void ConsumeConfirm() => handledFrame = Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => handledFrame = -1;
}
