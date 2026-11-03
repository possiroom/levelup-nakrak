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
        [SerializeField] private bool hideIconPlaceholder;
        [SerializeField] private bool keyItem;
        [Tooltip("Available only while exploring a memory.")]
        [SerializeField] private bool memoryOnly;
        [Tooltip("Removed from gameplay; identifier retained only to accept older saves.")]
        [SerializeField] private bool retired;
        [SerializeField] private DialogContainer inspectionDialogs;
        [SerializeField] private InventoryItem inspectionResult;
        [Tooltip("Legacy components automatically consolidated into this item when all are owned.")]
        [SerializeField] private InventoryItem[] combinedSources;
        [SerializeField] private ConditionID descriptionCondition;
        [TextArea(2, 6)] [SerializeField] private string changedDescription;
        [Min(1)] [SerializeField] private int maxStack = 99;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => descriptionCondition != null && ConditionManager.Instance != null &&
            ConditionManager.Instance.Get(descriptionCondition) != 0 && !string.IsNullOrEmpty(changedDescription) ? changedDescription : description;
        public StoryData InspectionDialog => inspectionDialogs != null ? inspectionDialogs.GetMatchingDialog() : null;
        public InventoryItem InspectionResult => inspectionResult;
        public InventoryItem[] CombinedSources => combinedSources;
        public Sprite Icon => icon;
        public bool HideIconPlaceholder => hideIconPlaceholder;
        public bool IsKeyItem => keyItem;
        public bool MemoryOnly => memoryOnly;
        public bool Retired => retired;
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
