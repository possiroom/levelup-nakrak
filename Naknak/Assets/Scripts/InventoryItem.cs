using UnityEngine;

namespace Naknak.Inventory
{
    [CreateAssetMenu(menuName = "Naknak/Inventory/Item", fileName = "Item_")]
    public sealed class InventoryItem : ScriptableObject
    {
        [Tooltip("Save identifier. Keep this unique and do not change it after release.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 6)] [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool keyItem;
        [Min(1)] [SerializeField] private int maxStack = 99;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool IsKeyItem => keyItem;
        public int MaxStack => Mathf.Max(1, maxStack);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N");
            maxStack = Mathf.Max(1, maxStack);
        }
#endif
    }
}
