using UnityEngine;
using Naknak.Inventory;

[CreateAssetMenu(menuName = "Naknak/Menu Configuration")]
public sealed class GameMenuConfiguration : ScriptableObject
{
    public string gameTitle = "낙낙";
    public string titleScene = "Title";
    public string startScene = "PrototypeScene";
    public Font font;
    public InventoryItem[] items = new InventoryItem[0];
}
