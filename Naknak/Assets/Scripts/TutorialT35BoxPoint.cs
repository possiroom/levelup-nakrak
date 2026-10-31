using UnityEngine;

/// <summary>Routes the player's normal investigation input to the blackout-search box.</summary>
[DisallowMultipleComponent]
public sealed class TutorialT35BoxPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private TutorialT35EventController controller;

    public void Interact()
    {
        if (controller != null) controller.Interact();
    }
}
