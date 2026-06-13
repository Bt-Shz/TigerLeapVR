using UnityEngine;
using System; // Required for the [Flags] attribute

// Define your categories here. 
// Using bit-shifting (1 << 0, 1 << 1) ensures each tag has a unique binary value.
[Flags]
public enum FoodCategoryMemoryMart
{
    None = 0,
    FreshFood = 1 << 0,   // 1
    Fruit = 1 << 1,       // 2
    Vegetables = 1 << 2,  // 4
    Protein = 1 << 3,     // 8
    Drink = 1 << 4,       // 16 (Example of how to add more)
    Snack = 1 << 5,       // 32
    Bakery = 1 << 6,       // 32
    PackagedFood = 1 << 7,
    Food = 1 << 8,

}

public class productInfoData : MonoBehaviour
{
    [Header("Product Information")]
    [Tooltip("The name of the item.")]
    public string itemName;

    // We replaced 'string category' with our new Flags Enum
    [Tooltip("The categories this item belongs to. You can select multiple!")]
    public FoodCategoryMemoryMart category;

    [Tooltip("The cost of the item.")]
    public int price;

    [Tooltip("The satisfaction rating of the item.")]
    public int satisfaction;

    [Tooltip("The visual icon/sprite for this item.")]
    public Sprite itemSprite;
}