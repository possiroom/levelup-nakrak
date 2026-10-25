using System;
using System.Collections.Generic;
using System.IO;
using Naknak.Inventory;
using UnityEngine;

// Copy with InventoryItem, InventoryModel and GameCheckpointStore into an isolated
// Unity project and run -executeMethod SaveFileSmokeVerification.Run -quit.
public static class SaveFileSmokeVerification
{
    [Serializable]
    private sealed class Result
    {
        public bool success;
        public string failure;
        public string saveFile;
        public int checks;
    }

    public static void Run()
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        if (!project.Replace('\\', '/').Contains("/Temp/SaveSmokeVerification"))
            throw new InvalidOperationException("Use the isolated smoke-test project.");
        var result = new Result();
        InventoryItem item = null;
        try
        {
            string directory = Path.Combine(project, "TestSaves", Guid.NewGuid().ToString("N"));
            item = ScriptableObject.CreateInstance<InventoryItem>();
            JsonUtility.FromJsonOverwrite("{\"id\":\"save-check-note\",\"maxStack\":99}", item);
            var inventory = new InventoryModel(new[] { item });
            Assert(inventory.Add(item.Id, 2), "Acquire items", result);
            var data = new GameCheckpoint
            {
                savedAt = DateTimeOffset.Now.ToString("O"), scene = "PrototypeScene",
                position = new Vector3(2.5f, 4, 0), facing = Vector2.left, floor = 2, health = 73,
                inventory = inventory.Capture(),
                conditions = new List<ConditionValue> { new ConditionValue { key = "save-check", value = 7 } },
                world = new List<WorldObjectState> { new WorldObjectState { path = "test-map|pickup", exists = true, active = false } }
            };
            GameCheckpointStore.WriteManual(1, data, directory);
            result.saveFile = GameCheckpointStore.ManualSlotPath(1, directory);
            Assert(File.Exists(result.saveFile), "Save file created", result);
            data.position = Vector3.zero;
            data.inventory[0].quantity = 99;
            data.conditions[0].value = 999;
            inventory.Remove(item.Id, 2);
            Assert(GameCheckpointStore.TryReadManual(1, out var loaded, out var error, directory), "Read saved file: " + error, result);
            Assert(loaded.position == new Vector3(2.5f, 4, 0) && loaded.floor == 2, "Position and floor retained", result);
            Assert(loaded.health == 73 && loaded.facing == Vector2.left, "Health and facing retained", result);
            inventory.Restore(loaded.inventory);
            Assert(inventory.Count(item.Id) == 2, "Inventory quantities restored", result);
            Assert(loaded.conditions[0].value == 7 && !loaded.world[0].active, "Story and world state retained", result);
            Assert(!File.Exists(result.saveFile + ".tmp"), "No incomplete save left behind", result);
            result.success = true;
            Debug.Log("[Save file check] PASS: " + result.checks + " checks using real Unity serialization.");
        }
        catch (Exception exception)
        {
            result.failure = exception.ToString();
            throw;
        }
        finally
        {
            if (item != null) UnityEngine.Object.DestroyImmediate(item);
            File.WriteAllText(Path.Combine(project, "save-file-report.json"), JsonUtility.ToJson(result, true));
        }
    }

    private static void Assert(bool condition, string message, Result result)
    {
        if (!condition) throw new InvalidOperationException(message);
        result.checks++;
    }
}
