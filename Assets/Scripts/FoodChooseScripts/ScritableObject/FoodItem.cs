using UnityEngine;

public enum FoodCategoryy
{
    Grains,
    VegFruit,
    Protein,
    Soy
}

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class FoodItemSOo : ScriptableObject
{
    public string foodName;
    public FoodCategory category;

    public float oil;
    public float sugar;
    public float protein;

    public GameObject foodPrefab; // 🔥 IMPORTANT
}