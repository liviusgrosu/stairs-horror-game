using UnityEngine;

public enum ItemType { Consumable, Material, GemSlot }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public string Name;
    [TextArea] public string Description;
    public Sprite Icon;
    public ItemType Type;
}
