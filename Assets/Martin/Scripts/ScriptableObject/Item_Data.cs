using UnityEngine;

[CreateAssetMenu(menuName = "SO/Item/Data")]
public class Item_Data : ScriptableObject
{
    [Tooltip("100-Keys, 200-collectables, 300-Lore")]
    public int ID;
    public ItemType type;
    public string itemName;
    public Sprite icon;
    [TextArea(2, 5)]
    public string description;
}

public enum ItemType
{
    None,
    Key,
    collectable,
    Lore
}
