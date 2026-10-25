// The console checks run the production inventory rules and file IO without starting Unity.
// Engine serialization, assets and the purchased XML saver have separate editor checks in InventoryTools.
using System.Text.Json;

namespace Naknak.Inventory
{
    public sealed class InventoryItem
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public bool IsKeyItem { get; set; }
        public int MaxStack { get; set; }
    }
}

namespace UnityEngine
{
    public struct Vector2 { public float x, y; }
    public struct Vector3 { public float x, y, z; }
    public static class Application { public static string persistentDataPath => System.IO.Path.GetTempPath(); }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value, bool pretty) => JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string text)
        {
            try { return JsonSerializer.Deserialize<T>(text, Options); }
            catch (JsonException error) { throw new System.ArgumentException("Invalid JSON", error); }
        }
    }
}
