using UnityEngine;

namespace Naknak.Inventory
{
    /// <summary>Put on an Interactable NPC-layer collider, or enable collectOnTouch for a 2D trigger.</summary>
    public sealed class InventoryPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private InventoryItem item;
        [Min(1)] [SerializeField] private int quantity = 1;
        [SerializeField] private bool collectOnTouch;
        private bool collected;

        public void Configure(InventoryItem definition, int count = 1)
        {
            item = definition;
            quantity = Mathf.Max(1, count);
        }

        public void Interact()
        {
            if (collected || item == null || GameMenuController.IsBlockingGameplay) return;
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;
            if (!GameMenuController.Inventory.Add(item.Id, quantity)) return;
            collected = true;
            // Keep the object for checkpoint restoration; disabling also hides its collider and icon.
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collectOnTouch && other.CompareTag("Player")) Interact();
        }
    }
}
