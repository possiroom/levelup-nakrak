using UnityEngine;

public sealed class DialogBlock : TriggerBlock
{
    [Header("Dialog Container")]
    [SerializeField] private DialogContainer dialogContainer;

    protected override void OnTriggered()
    {
        if (dialogContainer == null)
        {
            Debug.LogWarning($"[DialogBlock] 올바른 Container가 아닙니다. {gameObject.name}");
            return;
        }

        StoryData storyData = dialogContainer.GetMatchingDialog();

        if (storyData != null)
        {
            Debug.Log("DialogBlock 상호작용 storyID: " + storyData.name);
            GameEventBase evt = GameEventFactory.CreateDialogEvent(storyData);
            GameEventManager.Instance.Submit(evt);
        }
        else
        {
            Debug.LogWarning($"[DialogBlock] 컨테이너 내부 조건에 맞는 슬롯이 없습니다. {gameObject.name}");
        }
    }
}
