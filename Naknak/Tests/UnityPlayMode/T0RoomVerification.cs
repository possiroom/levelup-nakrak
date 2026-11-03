using System;
using System.Collections;
using System.Reflection;
using Naknak.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

// Real T0, dialogue, inventory and persistence logic. Only the menu selection UI
// and the fixture's existing gameplay dependencies are replaced with stand-ins.
public static class T0RoomVerification
{
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static InventoryItem Item(string id)
    {
        var item = ScriptableObject.CreateInstance<InventoryItem>();
        JsonUtility.FromJsonOverwrite("{\"id\":\"" + id + "\",\"keyItem\":true,\"maxStack\":1}", item);
        return item;
    }

    private static StoryData Story(string text)
    {
        var story = ScriptableObject.CreateInstance<StoryData>();
        story.text = text; story.isEnd = true;
        return story;
    }

    private static void CloseDialogue()
    {
        for (int i = 0; i < 3 && DialogManager.Instance.IsDialogOpen; i++) DialogManager.Instance.NextDialog();
    }

    private static TutorialRoom0 Room(InventoryItem brooch, InventoryItem box, ConditionID initialized,
        ConditionID packed, StoryData success, StoryData wrong, StoryData blocked, out SpriteRenderer sparkle)
    {
        var go = new GameObject("T0 Room Under Test");
        sparkle = new GameObject("T0 Sparkle").AddComponent<SpriteRenderer>();
        sparkle.transform.SetParent(go.transform);
        var room = go.AddComponent<TutorialRoom0>();
        Set(room, "brooch", brooch); Set(room, "packedBox", box);
        Set(room, "initialized", initialized); Set(room, "packed", packed); Set(room, "sparkle", sparkle);
        Set(room, "packedMessage", success); Set(room, "wrongItemMessage", wrong); Set(room, "exitBlockedMessage", blocked);
        return room;
    }

    public static IEnumerator Checks(Action<bool, string> assert)
    {
        CloseDialogue();
        yield return SceneLoadManager.Instance.LoadContent("TestB");
        yield return null;
        CloseDialogue();
        var brooch = Item("t0-brooch"); var box = Item("t0-packed-box"); var wrong = Item("t0-wrong");
        var initialized = ScriptableObject.CreateInstance<ConditionID>(); initialized.name = "T0InitTest";
        var packed = ScriptableObject.CreateInstance<ConditionID>(); packed.name = "T0PackedTest";
        var success = Story("packed"); var wrongMessage = Story("wrong"); var blocked = Story("blocked");
        GameMenuController.Instance = new GameMenuController();
        GameMenuController.Inventory = new InventoryModel(new[] { brooch, box, wrong });
        var inventory = GameMenuController.Inventory;
        ConditionManager.Instance.Set(initialized, 0); ConditionManager.Instance.Set(packed, 0);
        var room = Room(brooch, box, initialized, packed, success, wrongMessage, blocked, out var sparkle);
        yield return null;
        assert(inventory.Has(brooch.Id) && sparkle.enabled && !room.IsPacked, "T0 begins with brooch and visible sparkle");
        room.TryLeaveRoom();
        assert(DialogManager.Instance.IsDialogOpen && !SceneLoadManager.IsLoading, "T0 refuses early exit with existing dialogue");
        CloseDialogue();
        room.InteractWithBox();
        assert(GameMenuController.Instance.Selection != null, "T0 opens existing item selector");
        GameMenuController.Instance.CancelForTest();
        assert(inventory.Has(brooch.Id) && !room.IsPacked, "Cancelling T0 selection preserves brooch and exit lock");
        inventory.Add(wrong.Id);
        room.InteractWithBox(); GameMenuController.Instance.ChooseForTest(wrong);
        assert(inventory.Has(brooch.Id) && inventory.Has(wrong.Id) && !room.IsPacked && DialogManager.Instance.IsDialogOpen,
            "Wrong T0 item is not consumed and displays guidance");
        CloseDialogue();
        room.InteractWithBox(); GameMenuController.Instance.ChooseForTest(brooch);
        assert(!inventory.Has(brooch.Id) && inventory.Has(box.Id) && room.IsPacked && !sparkle.enabled,
            "T0 packing exchanges brooch for box and clears sparkle");
        assert(ConditionManager.Instance.Get(packed) == 1 && ConditionManager.Instance.Get(initialized) == 1,
            "T0 progression uses saved story conditions");
        CloseDialogue();
        var savedInventory = inventory.Capture(); var savedConditions = ConditionManager.Instance.CaptureValues();
        UnityEngine.Object.Destroy(room.gameObject); yield return null;
        inventory.Restore(null); ConditionManager.Instance.RestoreValues(null);
        SceneLoadManager.IsRestoringSession = true;
        room = Room(brooch, box, initialized, packed, success, wrongMessage, blocked, out sparkle);
        yield return null;
        assert(!inventory.Has(brooch.Id), "T0 does not seed items while save restoration is pending");
        inventory.Restore(savedInventory); ConditionManager.Instance.RestoreValues(savedConditions);
        SceneLoadManager.IsRestoringSession = false;
        yield return null;
        assert(room.IsPacked && inventory.Count(box.Id) == 1 && !inventory.Has(brooch.Id) && !sparkle.enabled,
            "Restored T0 keeps packed state without granting another brooch");
        // Directly requesting a connection must still honor its packed condition.
        var flow = ScriptableObject.CreateInstance<SceneFlowContainer>();
        var source = new SceneReference(); JsonUtility.FromJsonOverwrite("{\"path\":\"Assets/Scenes/TestB.unity\"}", source);
        var target = new SceneReference(); JsonUtility.FromJsonOverwrite("{\"path\":\"Assets/Scenes/TestA.unity\"}", target);
        var connection = new SceneFlowContainer.Connection { from = source, to = target };
        connection.conditions.Add(new DialogCondition { id = packed, compare = CompareType.IsTrue });
        flow.connections.Add(connection);
        var exit = room.gameObject.AddComponent<SceneTransitionPoint>(); exit.container = flow; exit.connectionId = connection.id;
        Set(room, "exit", exit);
        ConditionManager.Instance.Set(packed, 0);
        assert(!SceneLoadManager.Instance.Travel(flow, connection), "T0 connection cannot bypass packing condition");
        ConditionManager.Instance.Set(packed, 1);
        room.TryLeaveRoom();
        while (SceneLoadManager.IsLoading) yield return null;
        assert(SceneManager.GetSceneByName("TestA").isLoaded && !SceneManager.GetSceneByName("TestB").isLoaded,
            "Packed T0 exits through SceneLoadManager");
        UnityEngine.Object.Destroy(room.gameObject);
        GameMenuController.Instance = null; GameMenuController.Inventory = null;
    }
}
