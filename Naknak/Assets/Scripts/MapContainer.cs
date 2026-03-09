using UnityEngine;

public class MapContainer : MonoBehaviour
{
    public GridLayout GridLayout;
    public GameObject TriggerTilemap;
    public bool debugMode = false;
    private TriggerBlock[] triggerBlocks;

    public void TriggerDebug(bool activate)
    {
        foreach (TriggerBlock block in triggerBlocks)
        {   
            block.ActivateSprite(activate);
        }
    }

    private void Start()
    {
        triggerBlocks = TriggerTilemap.GetComponentsInChildren<TriggerBlock>();
        TriggerDebug(debugMode);
    }

    public ConditionToEvent GetC2E()
    {
        return gameObject.GetComponent<ConditionToEvent>();
    }
}
