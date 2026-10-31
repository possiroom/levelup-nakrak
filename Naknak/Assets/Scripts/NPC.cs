using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogContainer dialogContainer;
    [SerializeField] private bool allowDuringFirstMemory;
    public bool AllowDuringFirstMemory => allowDuringFirstMemory;

    public void Interact()
    {
        if (SceneLoadManager.IsFirstMemoryVisit && !allowDuringFirstMemory) return;
        if (dialogContainer == null)
        {
            Debug.LogWarning($"[NPC] 올바른 Container가 아닙니다. {gameObject.name}");
            return;
        }

        StoryData storyData = dialogContainer.GetMatchingDialog();

        if (storyData != null)
        {
            Debug.Log("[NPC] 상호작용 storyID: " + storyData.name);
            GameEventBase evt = GameEventFactory.CreateDialogEvent(storyData);
            GameEventManager.Instance.Submit(evt);

        }
        else
        {
            Debug.LogWarning($"[NPC] 컨테이너 내부 조건에 맞는 슬롯이 없습니다. {gameObject.name}");
        }
    }
}
