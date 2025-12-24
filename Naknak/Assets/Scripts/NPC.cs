using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogContainer dialogContainer;

    public void Interact()
    {
        if (dialogContainer == null)
        {
            Debug.LogWarning($"[NPC] DialogContainer is not set for {gameObject.name}");
            return;
        }

        StoryData storyData = dialogContainer.GetMatchingDialog();

        if (storyData != null)
        {
            Debug.Log("[NPC] Interact -> storyID: " + storyData.name);
            GameEventBase evt = GameEventFactory.CreateDialogEvent(storyData);
            GameEventManager.Instance.Submit(evt);
        }
        else
        {
            Debug.LogWarning($"[NPC] No matching dialog found in DialogContainer for {gameObject.name}");
        }
    }
}
