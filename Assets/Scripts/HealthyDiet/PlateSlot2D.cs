using TMPro;
using UnityEngine;

public class PlateSlot2d : MonoBehaviour
{
    public FoodCategory allowedCategory;

    [Header("Spawn Points")]
    public Transform[] platedSpawnPoints;

    [Header("Plate Indicator Visuals")]
    public SpriteRenderer plateSpriteRenderer;
    public Sprite defaultPlateSprite;
    public Sprite correctPlateSprite;
    public Sprite wrongPlateSprite;

    [Header("UI Text")]
    public TextMeshProUGUI plateCountText;

    private int currentItemCount = 0;

    void Start()
    {
        UpdatePlateText();
    }

    public void ReceiveFood(FoodItemSO foodData)
    {
        if (currentItemCount >= platedSpawnPoints.Length) return;

        Transform spawnPoint = platedSpawnPoints[currentItemCount];

        GameObject newPlatedFood = new GameObject("Plated_" + foodData.foodName);
        newPlatedFood.transform.position = spawnPoint.position;
        newPlatedFood.transform.SetParent(this.transform);

        SpriteRenderer sr = newPlatedFood.AddComponent<SpriteRenderer>();
        sr.sprite = foodData.platedSprite;
        sr.sortingOrder = 5;

        currentItemCount++;
        UpdatePlateText();

        // Pass the entire foodData object so the Game Manager can read Oil/Sugar floats
        FoodeChooseGameManager.Instance.HandleCorrectPlacement(foodData, transform.position);
    }

    public bool HasSpace()
    {
        return currentItemCount < platedSpawnPoints.Length;
    }

    private void UpdatePlateText()
    {
        if (plateCountText != null)
        {
            plateCountText.text = currentItemCount + "/" + platedSpawnPoints.Length;
        }
    }

    public void SetHoverVisual(bool isCorrect)
    {
        if (plateSpriteRenderer != null)
        {
            plateSpriteRenderer.sprite = isCorrect ? correctPlateSprite : wrongPlateSprite;
        }
    }

    public void ResetVisual()
    {
        if (plateSpriteRenderer != null && defaultPlateSprite != null)
        {
            plateSpriteRenderer.sprite = defaultPlateSprite;
        }
    }
}