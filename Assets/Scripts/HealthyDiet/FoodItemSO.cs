using UnityEngine;

public enum FoodCategory
{
    Grains,
    VegFruit,
    Protein,
    Soy
}

[CreateAssetMenu(fileName = "FoodItem", menuName = "Scriptable Objects/FoodItem")]
public class FoodItemSO : ScriptableObject
{
    public string foodName;
    public FoodCategory category;

    public float oil;
    public float sugar;
    public float protein;

    // 🔥 NEW: Check this box in the Inspector if the food is healthy
    public bool isHealthy;

    public GameObject foodPrefab;

    // 🔥 NEW: The sprite to show when the food is successfully plated
    [Header("Visuals")]
    public Sprite platedSprite;
}