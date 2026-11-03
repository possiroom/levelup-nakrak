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
        // Successful acquisition transactions only. Restoring a save never raises this event.
        public event Action Acquired;
        public bool IsMemorySpace { get; private set; }

        public void SetMemorySpace(bool active)
        {
            IsMemorySpace = active;
            if (!active) ClearMemoryItems();
        }

        public void ClearMemoryItems()
        {
            var ids = counts.Keys.Where(id => catalog[id].MemoryOnly).ToList();
            foreach (var id in ids) counts.Remove(id);
            if (ids.Count > 0) Changed?.Invoke();
        }

        private bool Available(InventoryItem item) => item != null && !item.Retired && (!item.MemoryOnly || IsMemorySpace);

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
        public int Count(string id) => Available(Find(id)) && counts.TryGetValue(id, out var count) ? count : 0;
        public bool Has(string id, int quantity = 1) => quantity > 0 && Count(id) >= quantity;

        // Addition/removal is atomic: an overflowing stack never partially consumes a pickup.
        public bool Add(string id, int quantity = 1)
        {
            var item = Find(id);
            if (quantity <= 0 || !Available(item) || quantity > item.MaxStack - Count(id)) return false;
            counts[id] = Count(id) + quantity;
            ConsolidateComponents();
            Changed?.Invoke();
            Acquired?.Invoke();
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

        // Investigation rewards are one transaction, including repeated item IDs.
        public bool TryAddAll(IEnumerable<InventoryEntry> rewards)
        {
            if (rewards == null) return false;
            var additions = new Dictionary<string, int>();
            foreach (var entry in rewards)
            {
                var item = entry == null ? null : Find(entry.itemId);
                if (!Available(item) || entry.quantity <= 0) return false;
                int pending = additions.TryGetValue(entry.itemId, out var count) ? count : 0;
                if (entry.quantity > item.MaxStack - Count(entry.itemId) - pending) return false;
                additions[entry.itemId] = pending + entry.quantity;
            }
            foreach (var pair in additions) counts[pair.Key] = Count(pair.Key) + pair.Value;
            ConsolidateComponents();
            if (additions.Count > 0) Changed?.Invoke();
            if (additions.Count > 0) Acquired?.Invoke();
            return true;
        }

        public List<InventoryEntry> Capture() => counts.Where(p => Available(catalog[p.Key])).OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new InventoryEntry(p.Key, p.Value)).ToList();

        // Item use must not consume the source if the result cannot be received.
        public bool TryExchange(string sourceId, string resultId)
        {
            var result = Find(resultId);
            if (sourceId == resultId || !Has(sourceId) || !Available(result) || Count(resultId) >= result.MaxStack)
                return false;
            int remaining = Count(sourceId) - 1;
            if (remaining == 0) counts.Remove(sourceId);
            else counts[sourceId] = remaining;
            counts[resultId] = Count(resultId) + 1;
            ConsolidateComponents();
            Changed?.Invoke();
            Acquired?.Invoke();
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
            foreach (var pair in replacement)
                if (!catalog[pair.Key].Retired) counts.Add(pair.Key, pair.Value);
            ConsolidateComponents();
            Changed?.Invoke();
        }

        private void ConsolidateComponents()
        {
            foreach (var result in catalog.Values)
            {
                var sources = result.CombinedSources;
                if (!Available(result) || sources == null || sources.Length < 2 ||
                    sources.Any(s => s == null || s.Id == result.Id || Find(s.Id) == null) ||
                    sources.Select(s => s.Id).Distinct().Count() != sources.Length) continue;
                int quantity = Math.Min(result.MaxStack - Count(result.Id), sources.Min(s => Count(s.Id)));
                if (quantity <= 0) continue;
                foreach (var source in sources)
                {
                    int remaining = Count(source.Id) - quantity;
                    if (remaining == 0) counts.Remove(source.Id);
                    else counts[source.Id] = remaining;
                }
                counts[result.Id] = Count(result.Id) + quantity;
            }
        }
    }
}
