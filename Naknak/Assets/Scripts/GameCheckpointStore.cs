using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Naknak.Inventory;

[Serializable]
public sealed class GameCheckpoint
{
    public int version = 1;
    public string savedAt;
    public string scene;
    public List<string> additiveScenes = new List<string>();
    public Vector3 position;
    public Vector2 facing;
    public int floor = 1;
    public int health = 100;
    public float playSeconds;
    public List<InventoryEntry> inventory = new List<InventoryEntry>();
    public List<ConditionValue> conditions = new List<ConditionValue>();
    public List<WorldObjectState> world = new List<WorldObjectState>();
    public List<MansionCheckpoint> mansions = new List<MansionCheckpoint>();
}

[Serializable]
public sealed class ConditionValue
{
    public string key;
    public int value;
}

[Serializable]
public sealed class WorldObjectState
{
    public string path;
    public bool exists;
    public bool active;
}

[Serializable]
public sealed class MansionCheckpoint
{
    public string path;
    public int room;
    public int phase;
    public int burningRoom;
    public bool seen;
    public List<int> deletedRooms = new List<int>();
}

/// <summary>Three automatic checkpoints and three independent manual saves.</summary>
public static class GameCheckpointStore
{
    public const int SlotCount = 3;
    public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Naknak", "Saves");
    public static string SlotPath(int slot, string saveDirectory = null)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(saveDirectory ?? DirectoryPath, "slot_" + slot + ".json");
    }

    public static string ManualSlotPath(int slot, string saveDirectory = null)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(saveDirectory ?? DirectoryPath, "manual_" + slot + ".json");
    }

    public static bool TryRead(int slot, out GameCheckpoint data, out string error, string saveDirectory = null)
        => TryReadFile(SlotPath(slot, saveDirectory), out data, out error);

    public static bool TryReadManual(int slot, out GameCheckpoint data, out string error, string saveDirectory = null)
        => TryReadFile(ManualSlotPath(slot, saveDirectory), out data, out error);

    private static bool TryReadFile(string path, out GameCheckpoint data, out string error)
    {
        data = null;
        error = null;
        try
        {
            if (!File.Exists(path)) return false;
            data = JsonUtility.FromJson<GameCheckpoint>(File.ReadAllText(path, Encoding.UTF8));
            Validate(data);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                          exception is ArgumentException || exception is InvalidOperationException)
        {
            data = null;
            error = exception.Message;
            return false;
        }
    }

    public static void Validate(GameCheckpoint data)
    {
        if (data == null || data.version != 1 || string.IsNullOrWhiteSpace(data.scene) ||
            !DateTimeOffset.TryParse(data.savedAt, out _) || data.floor < 1 || data.floor > 3 ||
            !Finite(data.position.x) || !Finite(data.position.y) || !Finite(data.position.z) ||
            !Finite(data.facing.x) || !Finite(data.facing.y) || !Finite(data.playSeconds) || data.playSeconds < 0 ||
            data.inventory == null || data.conditions == null || data.world == null ||
            data.mansions == null || data.additiveScenes == null)
            throw new ArgumentException("지원하지 않거나 손상된 저장 파일입니다.");
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public static void WriteManual(int slot, GameCheckpoint data, string saveDirectory = null)
    {
        string path = ManualSlotPath(slot, saveDirectory);
        Validate(data);
        Directory.CreateDirectory(saveDirectory ?? DirectoryPath);
        string pending = path + ".tmp";
        try
        {
            File.WriteAllText(pending, JsonUtility.ToJson(data, true), Encoding.UTF8);
            if (File.Exists(path)) File.Replace(pending, path, path + ".bak");
            else File.Move(pending, path);
        }
        finally
        {
            if (File.Exists(pending)) File.Delete(pending);
        }
    }

    public static void Write(GameCheckpoint data, string saveDirectory = null)
    {
        Validate(data);
        Directory.CreateDirectory(saveDirectory ?? DirectoryPath);
        // Write and flush a complete new checkpoint before replacing the current slot.
        string pending = SlotPath(1, saveDirectory) + ".tmp";
        File.WriteAllText(pending, JsonUtility.ToJson(data, true), Encoding.UTF8);
        string first = SlotPath(1, saveDirectory);
        // History changes only after the new file is complete. Never delete a user's saves on new game.
        if (File.Exists(SlotPath(2, saveDirectory))) File.Copy(SlotPath(2, saveDirectory), SlotPath(3, saveDirectory), true);
        if (File.Exists(first))
        {
            File.Copy(first, SlotPath(2, saveDirectory), true);
            File.Replace(pending, first, null);
        }
        else File.Move(pending, first);
    }
}
