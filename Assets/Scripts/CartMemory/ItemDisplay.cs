using UnityEngine;

public class ItemDisplay : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    // We store these so we know WHAT was clicked and WHO to tell
    private ItemData myData;
    private MemoryGameManager gameManager;

    public void Setup(ItemData data, MemoryGameManager manager)
    {
        myData = data;
        gameManager = manager;

        gameObject.name = data.itemName;

        if (spriteRenderer != null && data.itemSprite != null)
        {
            spriteRenderer.sprite = data.itemSprite;
        }
    }

    // This built-in Unity function triggers when a player clicks a 2D Collider
    private void OnMouseDown()
    {
        if (gameManager != null && myData != null)
        {
            gameManager.OnItemClicked(myData);
        }
    }
}