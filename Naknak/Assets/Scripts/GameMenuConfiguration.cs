using UnityEngine;
using Naknak.Inventory;

[CreateAssetMenu(menuName = "Naknak/Menu Configuration")]
public sealed class GameMenuConfiguration : ScriptableObject
{
    public string gameTitle = "낙낙";
    public string titleScene = "Title";
    public string startScene = "PrototypeScene";
    public Font font;
    [Tooltip("All successful item pickups and exchanges. A short chime is used when unassigned.")]
    public AudioClip itemAcquiredSound;
    [Range(0f, 1f)] public float itemAcquiredVolume = 0.6f;
    public InventoryItem[] items = new InventoryItem[0];
}
