using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public class SatisfactionManager : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard }

    [Header("UI Components")]
    [Tooltip("Panel to select Easy, Normal, or Hard")]
    public GameObject difficultyPanel;

    [Tooltip("The parent GameObject of your shopping panel UI.")]
    public GameObject shoppingPanel;

    [Header("Category Goal UI")]
    public TextMeshProUGUI categoryNameText;
    public TextMeshProUGUI categoryTotalPriceText;
    public TextMeshProUGUI categoryTotalSatisfactionText;
    public TextMeshProUGUI totalItemsToBuyText;

    [Header("Accumulated Totals UI (Main HUD)")]
    public TextMeshProUGUI runningSatisfactionText;
    public TextMeshProUGUI runningPriceText;

    [Header("Last Picked Item UI")]
    public TextMeshProUGUI totalsatisfactionText;
    public TextMeshProUGUI totalPriceText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Cart Panel UI")]
    public GameObject cartPanel;
    public Transform cartItemContainer;
    public GameObject cartItemPrefab;
    public GameObject dynamicCart;
    public TextMeshProUGUI cartTotalSatisfactionText;
    public TextMeshProUGUI cartTotalPriceText;

    [Header("Cart Goal Reminders")]
    public TextMeshProUGUI cartCategoryNameText;
    public TextMeshProUGUI cartItemsRemainingText;

    [Header("Settings")]
    public int maxPerCategory = 2;

    private Difficulty currentDifficulty = Difficulty.Easy;
    private int maxItemsToBuy;
    private int targetSatisfaction;
    private int startingBudget;

    private List<productInfoData> targetShoppingList = new List<productInfoData>();
    private List<productInfoData> purchasedItems = new List<productInfoData>();

    private int totalSatisfactionAccumulated = 0;
    private int totalPriceAccumulated = 0;
    private int totalItemsPickedCount = 0;

    [Header("Star Rating UI")]
    public UnityEngine.UI.Image[] starSlots;
    public Sprite glowStarSprite;
    public float starDelay = 0.25f;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (cartPanel != null) cartPanel.SetActive(false);
        if (shoppingPanel != null) shoppingPanel.SetActive(false);
        if (dynamicCart != null) dynamicCart.SetActive(false);

        if (difficultyPanel != null) difficultyPanel.SetActive(true);
    }

    public void SetDifficulty(int difficultyLevel)
    {
        currentDifficulty = (Difficulty)difficultyLevel;

        if (difficultyPanel != null) difficultyPanel.SetActive(false);

        GenerateRandomShoppingList();
        UpdateRunningTotalsUI();
    }

    public void StartShopping()
    {
        if (shoppingPanel != null) shoppingPanel.SetActive(false);
        if (dynamicCart != null) dynamicCart.SetActive(true);
    }

    private void GenerateRandomShoppingList()
    {
        productInfoData[] allSceneItems = FindObjectsOfType<productInfoData>();

        if (allSceneItems.Length == 0)
        {
            Debug.LogError("No food items in the scene!");
            return;
        }

        List<productInfoData> shuffledAllItems = allSceneItems.OrderBy(x => Random.value).ToList();
        targetShoppingList.Clear();

        string categoryDisplayText = "";

        if (currentDifficulty == Difficulty.Easy)
        {
            List<FoodCategoryMemoryMart> activeCategories = GetActiveCategories(allSceneItems);
            FoodCategoryMemoryMart chosenCategory = activeCategories[Random.Range(0, activeCategories.Count)];

            List<productInfoData> catItems = shuffledAllItems.Where(i => i.category.HasFlag(chosenCategory)).ToList();
            int amount = Mathf.Min(3, catItems.Count);

            targetShoppingList.AddRange(catItems.Take(amount));
            categoryDisplayText = chosenCategory.ToString();
        }
        else if (currentDifficulty == Difficulty.Normal)
        {
            List<FoodCategoryMemoryMart> activeCategories = GetActiveCategories(allSceneItems);
            int numCategoriesToPick = Random.Range(2, 4);

            List<FoodCategoryMemoryMart> chosenCategories = activeCategories.OrderBy(x => Random.value).Take(numCategoriesToPick).ToList();

            List<productInfoData> validItems = shuffledAllItems.Where(i => chosenCategories.Any(c => i.category.HasFlag(c))).ToList();
            int amount = Mathf.Clamp(Random.Range(4, 7), 0, validItems.Count);

            targetShoppingList.AddRange(validItems.Take(amount));
            categoryDisplayText = string.Join(", ", chosenCategories);
        }
        else if (currentDifficulty == Difficulty.Hard)
        {
            int amount = Mathf.Clamp(Random.Range(4, 7), 0, shuffledAllItems.Count);
            targetShoppingList.AddRange(shuffledAllItems.Take(amount));
            categoryDisplayText = "Any Category";
        }

        maxItemsToBuy = targetShoppingList.Count;

        int basePriceSum = targetShoppingList.Sum(i => i.price);
        int baseSatisfactionSum = targetShoppingList.Sum(i => i.satisfaction);

        // BUFFER REMOVED HERE
        targetSatisfaction = baseSatisfactionSum;
        startingBudget = basePriceSum;
        totalPriceAccumulated = 0;

        if (categoryNameText != null) categoryNameText.text = categoryDisplayText;
        if (cartCategoryNameText != null) cartCategoryNameText.text = categoryDisplayText;

        if (categoryTotalPriceText != null) categoryTotalPriceText.text = startingBudget.ToString();
        if (categoryTotalSatisfactionText != null) categoryTotalSatisfactionText.text = targetSatisfaction.ToString();
        if (totalItemsToBuyText != null) totalItemsToBuyText.text = maxItemsToBuy.ToString();

        if (totalsatisfactionText != null) totalsatisfactionText.text = targetSatisfaction.ToString();
        if (totalPriceText != null) totalPriceText.text = startingBudget.ToString();

        shoppingPanel.SetActive(true);
    }

    private List<FoodCategoryMemoryMart> GetActiveCategories(productInfoData[] items)
    {
        List<FoodCategoryMemoryMart> activeCategories = new List<FoodCategoryMemoryMart>();
        foreach (var item in items)
        {
            foreach (FoodCategoryMemoryMart flag in Enum.GetValues(typeof(FoodCategoryMemoryMart)))
            {
                if (flag != FoodCategoryMemoryMart.None && item.category.HasFlag(flag) && !activeCategories.Contains(flag))
                {
                    activeCategories.Add(flag);
                }
            }
        }
        return activeCategories;
    }

    public void BuyItem(productInfoData clickedItem)
    {
        if (totalItemsPickedCount >= maxItemsToBuy || (cartPanel != null && cartPanel.activeSelf)) return;

        totalItemsPickedCount++;
        totalSatisfactionAccumulated += clickedItem.satisfaction;
        totalPriceAccumulated += clickedItem.price;
        purchasedItems.Add(clickedItem);

        UpdateRunningTotalsUI();

        if (totalItemsPickedCount == maxItemsToBuy)
        {
            OpenCartPanel();
        }
    }

    public void RemoveItemFromCart(productInfoData itemToRemove)
    {
        if (!purchasedItems.Contains(itemToRemove)) return;

        purchasedItems.Remove(itemToRemove);
        totalItemsPickedCount--;
        totalSatisfactionAccumulated -= itemToRemove.satisfaction;
        totalPriceAccumulated -= itemToRemove.price;

        UpdateRunningTotalsUI();

        if (cartPanel != null) cartPanel.SetActive(false);
    }

    public void OpenCartPanel()
    {
        if (cartPanel == null) return;

        cartPanel.SetActive(true);
        UpdateRunningTotalsUI();

        foreach (Transform child in cartItemContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (productInfoData item in purchasedItems)
        {
            GameObject spawnedItem = Instantiate(cartItemPrefab, cartItemContainer);
            CartItemUI itemUI = spawnedItem.GetComponent<CartItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(item, this);
            }
        }
    }

    public void ConfirmCheckout()
    {
        if (cartPanel != null) cartPanel.SetActive(false);
        if (dynamicCart != null) dynamicCart.SetActive(false);
        CalculateCheckoutResults();
    }

    private void UpdateRunningTotalsUI()
    {
        if (runningSatisfactionText != null) runningSatisfactionText.text = totalSatisfactionAccumulated.ToString();
        if (runningPriceText != null) runningPriceText.text = totalPriceAccumulated.ToString();

        // --- THE NEW LINE: Update the Cart's Satisfaction Text ---
        if (cartTotalSatisfactionText != null) cartTotalSatisfactionText.text = totalSatisfactionAccumulated.ToString();

        int currentCartPriceSum = purchasedItems.Sum(item => item.price);
        if (cartTotalPriceText != null) cartTotalPriceText.text = currentCartPriceSum.ToString();

        if (cartItemsRemainingText != null)
        {
            int remaining = Mathf.Max(0, maxItemsToBuy - totalItemsPickedCount);
            cartItemsRemainingText.text = remaining.ToString();
        }
    }

    private void CalculateCheckoutResults()
    {
        if (shoppingPanel != null) shoppingPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        bool[] matchedTargets = new bool[targetShoppingList.Count];
        int correctCount = 0;
        int correctSatisfactionOnly = 0;

        foreach (var purchased in purchasedItems)
        {
            for (int i = 0; i < targetShoppingList.Count; i++)
            {
                if (!matchedTargets[i] && purchased.category == targetShoppingList[i].category)
                {
                    matchedTargets[i] = true;
                    correctCount++;
                    correctSatisfactionOnly += purchased.satisfaction;
                    break;
                }
            }
        }

        int totalCostAllItems = totalPriceAccumulated;

        int starCount = 0;

        if (correctCount == maxItemsToBuy) starCount++;
        if (correctSatisfactionOnly <= targetSatisfaction) starCount++;
        if (totalPriceAccumulated <= startingBudget) starCount++;

        StartCoroutine(AnimateStarsRoutine(starCount));
    }

    private IEnumerator AnimateStarsRoutine(int starsEarned)
    {
        foreach (var slot in starSlots)
        {
            if (slot == null) continue;
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < starsEarned; i++)
        {
            if (i >= starSlots.Length || starSlots[i] == null || glowStarSprite == null) break;

            GameObject glowObj = new GameObject("DynamicGlowStar", typeof(UnityEngine.UI.Image));
            glowObj.transform.SetParent(starSlots[i].transform, false);

            UnityEngine.UI.Image glowImage = glowObj.GetComponent<UnityEngine.UI.Image>();
            glowImage.sprite = glowStarSprite;

            RectTransform rectTransform = glowObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;

            Transform starTransform = glowObj.transform;
            float elapsed = 0f;

            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.15f;
                starTransform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f), t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                starTransform.localScale = Vector3.Lerp(new Vector3(1.3f, 1.3f, 1.3f), Vector3.one, t);
                yield return null;
            }

            starTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(starDelay);
        }
    }

    public void ResetGame()
    {
        targetShoppingList.Clear();
        purchasedItems.Clear();
        totalSatisfactionAccumulated = 0;
        totalItemsPickedCount = 0;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (cartPanel != null) cartPanel.SetActive(false);
        if (dynamicCart != null) dynamicCart.SetActive(false);
        if (shoppingPanel != null) shoppingPanel.SetActive(false);

        if (difficultyPanel != null) difficultyPanel.SetActive(true);
    }
}