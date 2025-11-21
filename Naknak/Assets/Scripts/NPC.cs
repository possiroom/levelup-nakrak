using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string storyID;

    public void Interact()
    {
        Debug.Assert(true, "[NPC] id: " + storyID);
        GameEventBase evt = GameEventFactory.CreateDialogEvent(storyID);
        GameEventManager.Instance.Submit(evt);
    }
}
