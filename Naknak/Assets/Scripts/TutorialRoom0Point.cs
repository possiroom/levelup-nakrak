using UnityEngine;

public sealed class TutorialRoom0Point : MonoBehaviour, IInteractable
{
    public enum PointAction { JewelryBox, Exit }
    [SerializeField] private TutorialRoom0 room;
    [SerializeField] private PointAction action;

    public void Interact()
    {
        if (room == null) return;
        if (action == PointAction.JewelryBox) room.InteractWithBox();
        else room.TryLeaveRoom();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = action == PointAction.Exit ? Color.cyan : Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.7f);
    }
}
