using System;
using System.Collections.Generic;
using System.Linq;

namespace Naknak.Inventory
{
    [Serializable]
    public sealed class InventoryEntry
    {
        public string itemId;
        public int quantity;
        public InventoryEntry() { }
        public InventoryEntry(string id, int count) { itemId = id; quantity = count; }
    }

    /// <summary>Inventory rules independent of UI, scenes and save files.</summary>
    public sealed class InventoryModel
    {
        private readonly Dictionary<string, InventoryItem> catalog;
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        public event Action Changed;

        public InventoryModel(IEnumerable<InventoryItem> items)
        {
            catalog = new Dictionary<string, InventoryItem>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                    throw new ArgumentException("Items must have a non-empty save identifier.");
                if (catalog.ContainsKey(item.Id))
                    throw new ArgumentException("Duplicate inventory item ID: " + item.Id);
                catalog.Add(item.Id, item);
            }
        }

        public InventoryItem Find(string id) => id != null && catalog.TryGetValue(id, out var item) ? item : null;
        public int Count(string id) => id != null && counts.TryGetValue(id, out var count) ? count : 0;
        public bool Has(string id, int quantity = 1) => quantity > 0 && Count(id) >= quantity;

        // Addition/removal is atomic: an overflowing stack never partially consumes a pickup.
        public bool Add(string id, int quantity = 1)
        {
            var item = Find(id);
            if (quantity <= 0 || item == null || quantity > item.MaxStack - Count(id)) return false;
            counts[id] = Count(id) + quantity;
            Changed?.Invoke();
            return true;
        }

        public bool Remove(string id, int quantity = 1)
        {
            if (!Has(id, quantity)) return false;
            int remaining = Count(id) - quantity;
            if (remaining == 0) counts.Remove(id);
            else counts[id] = remaining;
            Changed?.Invoke();
            return true;
        }

        public List<InventoryEntry> Capture() => counts.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new InventoryEntry(p.Key, p.Value)).ToList();

        // Item use must not consume the source if the result cannot be received.
        public bool TryExchange(string sourceId, string resultId)
        {
            var result = Find(resultId);
            if (sourceId == resultId || !Has(sourceId) || result == null || Count(resultId) >= result.MaxStack)
                return false;
            int remaining = Count(sourceId) - 1;
            if (remaining == 0) counts.Remove(sourceId);
            else counts[sourceId] = remaining;
            counts[resultId] = Count(resultId) + 1;
            Changed?.Invoke();
            return true;
        }

        public List<InventoryItem> Items(bool keyItems) => catalog.Values
            .Where(item => item.IsKeyItem == keyItems && Count(item.Id) > 0)
            .OrderBy(item => item.DisplayName, StringComparer.Ordinal).ThenBy(item => item.Id).ToList();

        public void Restore(IEnumerable<InventoryEntry> entries)
        {
            var replacement = new Dictionary<string, int>();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    var item = entry == null ? null : Find(entry.itemId);
                    if (item == null || entry.quantity <= 0 || entry.quantity > item.MaxStack ||
                        replacement.ContainsKey(entry.itemId))
                        throw new ArgumentException("Invalid or unavailable inventory item in save data.");
                    replacement.Add(entry.itemId, entry.quantity);
                }
            }
            counts.Clear();
            foreach (var pair in replacement) counts.Add(pair.Key, pair.Value);
            Changed?.Invoke();
        }
    }
}
