using UnityEngine;

public class Test1 : MonoBehaviour
{
    public bool check = false;
    private AIState state;
    int currentRoomID;
    int periousRoomID;

    AIManager aimanager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        aimanager = transform.parent.Find("AI Manager").GetComponent<AIManager>();

        currentRoomID = aimanager.ReturnPlayerRoomID();
        periousRoomID = aimanager.ReturnPlayerRoomID();
    }

    // Update is called once per frame
    void Update()
    {
        if (check)
            return;

        Debug.Log(currentRoomID);
        currentRoomID = aimanager.ReturnPlayerRoomID();

        if(currentRoomID != periousRoomID)
        {
            Debug.Log("적을 스폰했습니다.");

            aimanager.EnemyEvent(currentRoomID);

            check = true;

        }
    }
}
