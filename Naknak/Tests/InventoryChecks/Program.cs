using System;
using System.Collections.Generic;
using System.IO;
using Naknak.Inventory;

static class Program
{
    private static int assertions;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions++;
    }

    static void Reject(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { assertions++; return; }
        throw new Exception(message);
    }

    static void Main()
    {
        var memoryBrooch = new InventoryItem { Id = "memory-brooch", MaxStack = 1, IsKeyItem = true, MemoryOnly = true };
        var memoryBox = new InventoryItem { Id = "memory-box", MaxStack = 1, IsKeyItem = true, MemoryOnly = true };
        var evidence = new InventoryItem { Id = "evidence", MaxStack = 99, IsKeyItem = true };
        var scoped = new InventoryModel(new[] { memoryBrooch, memoryBox, evidence });
        Check(!scoped.Add(memoryBrooch.Id), "Memory items cannot be acquired in reality");
        Check(!scoped.TryAddAll(new[] { new InventoryEntry(memoryBrooch.Id, 1), new InventoryEntry(evidence.Id, 1) }) &&
            !scoped.Has(evidence.Id), "Out-of-memory reward rejection is atomic");
        scoped.SetMemorySpace(true);
        Check(scoped.Add(memoryBrooch.Id) && scoped.Add(evidence.Id), "Memory permits brooch and persistent evidence");
        Check(scoped.TryExchange(memoryBrooch.Id, memoryBox.Id), "Packing works inside memory");
        var memorySave = scoped.Capture();
        scoped.SetMemorySpace(false);
        Check(!scoped.Has(memoryBox.Id) && scoped.Items(true).Count == 1 && scoped.Has(evidence.Id), "Leaving memory removes only scoped items");
        Check(!scoped.TryExchange(evidence.Id, memoryBox.Id) && scoped.Has(evidence.Id), "Memory exchange cannot consume evidence in reality");
        scoped.Restore(memorySave);
        Check(!scoped.Has(memoryBox.Id) && scoped.Capture().Count == 1, "Legacy reality save cannot expose memory items");
        scoped.SetMemorySpace(true);
        Check(scoped.Has(memoryBox.Id), "Loading inside memory preserves the saved box phase");
        scoped.SetMemorySpace(false); scoped.SetMemorySpace(true);
        Check(!scoped.Has(memoryBox.Id) && scoped.Has(evidence.Id), "Reentry preserves evidence without leaking old boxes");
        var note = new InventoryItem { Id = "note", DisplayName = "쪽지", MaxStack = 5 };
        var key = new InventoryItem { Id = "key", DisplayName = "열쇠", MaxStack = 1, IsKeyItem = true };
        var inventory = new InventoryModel(new[] { note, key });
        int changes = 0;
        inventory.Changed += () => changes++;
        Check(!inventory.Add(null) && !inventory.Add("missing"), "Unknown items");
        Check(!inventory.Add("note", 0) && !inventory.Add("note", -1), "Positive quantities");
        Check(inventory.Add("note", 4) && inventory.Count("note") == 4, "Acquire stack");
        Check(!inventory.Add("note", 2) && !inventory.Add("note", int.MaxValue), "Reject overflow");
        Check(inventory.Count("note") == 4 && changes == 1, "Failed additions are atomic");
        Check(!inventory.Remove("note", 5) && !inventory.Remove("note", 0), "Reject invalid removal");
        Check(inventory.Remove("note", 3) && inventory.Count("note") == 1, "Remove partial stack");
        Check(inventory.Add("key") && !inventory.Add("key"), "Unique key item");
        Check(inventory.Items(true).Count == 1 && inventory.Items(false).Count == 1, "Category filtering");
        var snapshot = inventory.Capture();
        snapshot[0].quantity = 1000;
        Check(inventory.Count("key") == 1, "Snapshot isolation");
        Reject(() => inventory.Restore(new[] { new InventoryEntry("missing", 1) }), "Unknown restore");
        Reject(() => inventory.Restore(new[] { new InventoryEntry("note", 1), new InventoryEntry("note", 1) }), "Duplicate restore");
        Reject(() => inventory.Restore(new[] { new InventoryEntry("note", 6) }), "Overfull restore");
        Check(inventory.Count("note") == 1 && inventory.Count("key") == 1, "Invalid restore is atomic");
        Reject(() => new InventoryModel(new[] { note, note }), "Duplicate catalog IDs");
        var restored = new InventoryModel(new[] { note, key });
        restored.Restore(inventory.Capture());
        Check(restored.Has("note") && restored.Has("key"), "Inventory restore");
        Check(restored.Remove("note") && restored.Capture().Count == 1, "Zero stacks disappear");
        var packing = new InventoryModel(new[] { note, key });
        int packingChanges = 0;
        packing.Changed += () => packingChanges++;
        Check(!packing.TryExchange("note", "key"), "Packing requires the source item");
        packing.Add("note", 2);
        Check(!packing.TryExchange("note", "missing") && !packing.TryExchange("note", "note"), "Invalid packing result rejected");
        Check(packing.Count("note") == 2 && packingChanges == 1, "Failed packing preserves inventory");
        Check(packing.TryExchange("note", "key") && packing.Count("note") == 1 && packing.Has("key"), "Packing consumes one source and grants its result");
        Check(packingChanges == 2, "Packing sends one complete inventory update");
        Check(!packing.TryExchange("note", "key") && packing.Count("note") == 1 && packingChanges == 2, "Full result stack cannot consume the source");
        var packingRestored = new InventoryModel(new[] { note, key });
        packingRestored.Restore(packing.Capture());
        Check(packingRestored.Has("key") && packingRestored.Count("note") == 1, "Packed inventory survives restoration");
        var rewards = new InventoryModel(new[] { note, key });
        int rewardChanges = 0;
        rewards.Changed += () => rewardChanges++;
        Check(!rewards.TryAddAll(new[] { new InventoryEntry("note", 1), new InventoryEntry("missing", 1) }) && rewards.Count("note") == 0,
            "Unknown investigation reward cannot partially grant items");
        Check(!rewards.TryAddAll(new[] { new InventoryEntry("note", 3), new InventoryEntry("note", 3) }) && rewards.Count("note") == 0,
            "Repeated reward IDs obey aggregate stack capacity");
        Check(rewards.TryAddAll(new[] { new InventoryEntry("note", 2), new InventoryEntry("key", 1) }) && rewardChanges == 1,
            "Multiple investigation rewards send one atomic update");
        Check(!rewards.TryAddAll(new[] { new InventoryEntry("note", 1), new InventoryEntry("key", 1) }) && rewards.Count("note") == 2 && rewardChanges == 1,
            "Full reward stack preserves every other item");
        Check(!rewards.TryAddAll(new[] { new InventoryEntry("note", -1) }) && !rewards.TryAddAll(null), "Invalid reward inputs rejected");
        string directory = Path.Combine(Path.GetTempPath(), "NaknakChecks_" + Guid.NewGuid().ToString("N"));
        try
        {
            var checkpoint = new GameCheckpoint
            {
                scene = "PrototypeScene", savedAt = DateTimeOffset.Now.ToString("O"),
                inventory = inventory.Capture(), floor = 2,
                conditions = new List<ConditionValue> { new ConditionValue { key = "event", value = 7 } },
                world = new List<WorldObjectState> { new WorldObjectState { path = "pickup", exists = true, active = false } }
            };
            Check(!GameCheckpointStore.TryRead(1, out _, out var absentError, directory) && absentError == null, "Empty slot");
            for (int i = 1; i <= 4; i++) { checkpoint.playSeconds = i; GameCheckpointStore.Write(checkpoint, directory); }
            for (int slot = 1; slot <= 3; slot++)
            {
                bool read = GameCheckpointStore.TryRead(slot, out var data, out var readError, directory);
                Check(read, "Read slot " + slot + ": " + readError);
                Check(data.playSeconds == 5 - slot && data.floor == 2, "Rotation order");
                Check(data.conditions[0].value == 7 && !data.world[0].active, "World state survives file IO");
                restored.Restore(data.inventory);
                Check(restored.Count("note") == 1 && restored.Count("key") == 1, "Inventory survives file IO");
            }
            foreach (string corrupt in new[] { "not json", "null", "{}", "{\"version\":999}" })
            {
                File.WriteAllText(GameCheckpointStore.SlotPath(2, directory), corrupt);
                Check(!GameCheckpointStore.TryRead(2, out _, out var error, directory) && error != null, "Reject corrupt save");
            }
            Check(GameCheckpointStore.TryRead(1, out _, out _, directory), "Other slots survive corruption");
            checkpoint.playSeconds = float.NaN;
            Reject(() => GameCheckpointStore.Write(checkpoint, directory), "Reject nonfinite state before replacing save");
            Check(GameCheckpointStore.TryRead(1, out var current, out _, directory) && current.playSeconds == 4, "Invalid write preserves current slot");
            checkpoint.playSeconds = 10;
            GameCheckpointStore.WriteManual(1, checkpoint, directory);
            checkpoint.playSeconds = 20;
            GameCheckpointStore.WriteManual(2, checkpoint, directory);
            Check(!GameCheckpointStore.TryReadManual(3, out _, out var manualEmptyError, directory) && manualEmptyError == null, "Empty manual slot");
            checkpoint.playSeconds = 11;
            GameCheckpointStore.WriteManual(1, checkpoint, directory);
            Check(GameCheckpointStore.TryReadManual(1, out var manual, out _, directory) && manual.playSeconds == 11, "Manual overwrite");
            Check(GameCheckpointStore.TryReadManual(2, out var otherManual, out _, directory) && otherManual.playSeconds == 20, "Manual slots are independent");
            Check(GameCheckpointStore.TryRead(1, out var automatic, out _, directory) && automatic.playSeconds == 4, "Manual save preserves automatic slot");
            var backup = UnityEngine.JsonUtility.FromJson<GameCheckpoint>(File.ReadAllText(GameCheckpointStore.ManualSlotPath(1, directory) + ".bak"));
            Check(backup.playSeconds == 10, "Overwrite keeps previous manual record as backup");
            checkpoint.playSeconds = 99;
            GameCheckpointStore.Write(checkpoint, directory);
            Check(GameCheckpointStore.TryReadManual(1, out manual, out _, directory) && manual.playSeconds == 11, "Automatic rotation preserves manual save");
            Reject(() => GameCheckpointStore.WriteManual(0, checkpoint, directory), "Reject invalid manual slot");
            Reject(() => GameCheckpointStore.WriteManual(4, checkpoint, directory), "Reject out-of-range manual slot");
            checkpoint.playSeconds = float.NaN;
            Reject(() => GameCheckpointStore.WriteManual(1, checkpoint, directory), "Validate before replacing manual record");
            Check(GameCheckpointStore.TryReadManual(1, out manual, out _, directory) && manual.playSeconds == 11, "Invalid manual write preserves saved record");
            File.WriteAllText(GameCheckpointStore.ManualSlotPath(2, directory), "{broken");
            Check(!GameCheckpointStore.TryReadManual(2, out _, out var manualError, directory) && manualError != null, "Reject corrupted manual save");
            Check(GameCheckpointStore.TryReadManual(1, out _, out _, directory) && GameCheckpointStore.TryRead(1, out _, out _, directory), "Manual corruption is isolated");
        }
        finally
        {
            for (int slot = 1; slot <= 3; slot++)
            {
                string file = GameCheckpointStore.SlotPath(slot, directory);
                if (File.Exists(file)) File.Delete(file);
                foreach (string suffix in new[] { "", ".bak", ".tmp" })
                {
                    string manualFile = GameCheckpointStore.ManualSlotPath(slot, directory) + suffix;
                    if (File.Exists(manualFile)) File.Delete(manualFile);
                }
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
        Console.WriteLine("PASS: " + assertions + " inventory and checkpoint assertions (engine-independent checks).");
    }
}
