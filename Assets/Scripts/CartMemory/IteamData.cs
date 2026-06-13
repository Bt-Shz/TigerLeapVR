using UnityEngine;

public enum ItemType
{
    Food,
    Grocery,
    Medical,
    Energy
}

[CreateAssetMenu(fileName = "New Item", menuName = "Memory Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public Sprite itemSprite;

    [Header("Display")]
    [Tooltip("The prefab that has the ItemDisplay script attached")]
    public GameObject itemPrefab; // NEW: The specific prefab for this item
}