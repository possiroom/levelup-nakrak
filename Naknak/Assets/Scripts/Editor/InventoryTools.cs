using System;
using System.Collections.Generic;
using System.IO;
using CitrioN.SettingsMenuCreator;
using Naknak.Inventory;
using UnityEditor;
using UnityEngine;

public static class InventoryTools
{
    [MenuItem("Tools/Naknak/Save/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        Directory.CreateDirectory(GameCheckpointStore.DirectoryPath);
        EditorUtility.RevealInFinder(GameCheckpointStore.DirectoryPath);
    }

    [MenuItem("Tools/Naknak/Inventory/Give Sample Items (Play Mode)")]
    public static void GiveSampleItems()
    {
        if (!Application.isPlaying || GameMenuController.Instance == null)
        { Debug.Log("게임을 실행한 뒤 이 메뉴를 사용하세요."); return; }
        var configuration = Resources.Load<GameMenuConfiguration>("NaknakMenu");
        foreach (var item in configuration.items)
            if (item != null) GameMenuController.Inventory.Add(item.Id);
    }

    [MenuItem("GameObject/Naknak/Inventory Pickup", false, 20)]
    public static void CreatePickup()
    {
        var configuration = Resources.Load<GameMenuConfiguration>("NaknakMenu");
        if (configuration == null || configuration.items.Length == 0)
        { Debug.LogWarning("NaknakMenu 설정에 먼저 아이템을 등록하세요."); return; }
        var target = new GameObject("Inventory Pickup");
        Undo.RegisterCreatedObjectUndo(target, "Create inventory pickup");
        GameObjectUtility.SetParentAndAlign(target, Selection.activeGameObject);
        target.tag = "Interactable";
        int layer = LayerMask.NameToLayer("Col 1F");
        if (layer >= 0) target.layer = layer;
        var sprite = target.AddComponent<SpriteRenderer>();
        sprite.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sprite.color = new Color(0.8f, 0.72f, 0.42f);
        sprite.sortingOrder = 10;
        target.transform.localScale = new Vector3(0.5f, 0.5f, 1);
        var collider = target.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        target.AddComponent<InventoryPickup>().Configure(configuration.items[0]);
        Selection.activeGameObject = target;
    }

    [MenuItem("Tools/Naknak/Inventory/Run Checks")]
    public static void RunChecks()
    {
        var regular = ScriptableObject.CreateInstance<InventoryItem>();
        var key = ScriptableObject.CreateInstance<InventoryItem>();
        var collection = ScriptableObject.CreateInstance<SettingsCollection>();
        string directory = Path.Combine(Path.GetTempPath(), "NaknakInventoryChecks_" + Guid.NewGuid().ToString("N"));
        try
        {
            JsonUtility.FromJsonOverwrite("{\"id\":\"test-note\",\"displayName\":\"쪽지\",\"maxStack\":3}", regular);
            JsonUtility.FromJsonOverwrite("{\"id\":\"test-key\",\"displayName\":\"열쇠\",\"keyItem\":true,\"maxStack\":1}", key);
            var model = new InventoryModel(new[] { regular, key });
            Check(!model.Add("unknown"), "Unknown items must be rejected");
            Check(!model.Add(regular.Id, 0), "Zero quantities must be rejected");
            Check(model.Add(regular.Id, 2), "Acquire stack");
            Check(!model.Add(regular.Id, int.MaxValue) && model.Count(regular.Id) == 2, "Overflow must be atomic");
            Check(!model.Remove(regular.Id, 3) && model.Count(regular.Id) == 2, "Insufficient removal must be atomic");
            Check(model.Add(key.Id) && !model.Add(key.Id), "Unique key item");
            Check(model.Items(false).Count == 1 && model.Items(true).Count == 1, "Separate tabs");
            var snapshot = model.Capture();
            snapshot[0].quantity = 99;
            Check(model.Count(key.Id) == 1, "Snapshots must not expose mutable internal state");
            try { model.Restore(new[] { new InventoryEntry("unknown", 1) }); throw new Exception("Invalid restore accepted"); }
            catch (ArgumentException) { Check(model.Count(regular.Id) == 2, "Invalid restores preserve inventory"); }
            var data = new GameCheckpoint
            {
                savedAt = DateTimeOffset.Now.ToString("O"), scene = "PrototypeScene", inventory = model.Capture(),
                position = new Vector3(2, 3, 0), floor = 2,
                conditions = new List<ConditionValue> { new ConditionValue { key = "met-owner", value = 1 } },
                world = new List<WorldObjectState> { new WorldObjectState { path = "map|pickup", exists = true, active = false } }
            };
            for (int i = 1; i <= 4; i++) { data.playSeconds = i; GameCheckpointStore.Write(data, directory); }
            for (int slot = 1; slot <= 3; slot++)
            {
                Check(GameCheckpointStore.TryRead(slot, out var saved, out _, directory), "Read rotated slot");
                Check(saved.playSeconds == 5 - slot && saved.floor == 2 && saved.position.y == 3, "Rotation and position");
                Check(!saved.world[0].active && saved.conditions[0].value == 1, "World and condition restoration data");
                new InventoryModel(new[] { regular, key }).Restore(saved.inventory);
            }
            data.playSeconds = 10;
            GameCheckpointStore.WriteManual(1, data, directory);
            data.playSeconds = 20;
            GameCheckpointStore.WriteManual(2, data, directory);
            data.playSeconds = 11;
            GameCheckpointStore.WriteManual(1, data, directory);
            Check(GameCheckpointStore.TryReadManual(1, out var manual, out _, directory) && manual.playSeconds == 11, "Manual slot overwrite");
            Check(GameCheckpointStore.TryReadManual(2, out var secondManual, out _, directory) && secondManual.playSeconds == 20, "Independent manual slots");
            Check(GameCheckpointStore.TryRead(1, out var automatic, out _, directory) && automatic.playSeconds == 4, "Manual saves preserve automatic records");
            data.playSeconds = 99;
            GameCheckpointStore.Write(data, directory);
            Check(GameCheckpointStore.TryReadManual(1, out manual, out _, directory) && manual.playSeconds == 11, "Automatic saves preserve manual records");
            File.WriteAllText(GameCheckpointStore.SlotPath(2, directory), "{broken");
            Check(!GameCheckpointStore.TryRead(2, out _, out var error, directory) && error != null, "Corrupt save handling");
            Check(GameCheckpointStore.TryRead(1, out _, out _, directory), "Corruption does not affect other slots");
            collection.activeSettingValues["MasterVolume"] = 0.6f;
            var xml = XmlUtility_Settings.GetSaveString(collection, null);
            Check(Mathf.Approximately((float)XmlUtility_Settings.LoadFromText(xml)["MasterVolume"], 0.6f), "Purchased settings saver round trip");
            var config = Resources.Load<GameMenuConfiguration>("NaknakMenu");
            Check(config != null && config.font != null && config.items.Length > 0, "Menu assets");
            new InventoryModel(config.items);
            Debug.Log("[Naknak Inventory] All inventory, checkpoint rotation, corruption, and options checks passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(regular);
            UnityEngine.Object.DestroyImmediate(key);
            UnityEngine.Object.DestroyImmediate(collection);
            // Delete only the individual files created in this unique test directory.
            for (int slot = 1; slot <= 3; slot++)
            {
                string path = GameCheckpointStore.SlotPath(slot, directory);
                if (File.Exists(path)) File.Delete(path);
                foreach (string suffix in new[] { "", ".bak", ".tmp" })
                {
                    string manualPath = GameCheckpointStore.ManualSlotPath(slot, directory) + suffix;
                    if (File.Exists(manualPath)) File.Delete(manualPath);
                }
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
