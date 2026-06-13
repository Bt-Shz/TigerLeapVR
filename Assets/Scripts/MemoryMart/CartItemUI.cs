using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartItemUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI itemNameText; // <-- NEW: Added for Item Name
    public Image itemImage;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI satisfactionText;
    public Button deleteButton;

    private productInfoData associatedItem;
    private SatisfactionManager manager;

    // Called dynamically when the prefab is spawned in the cart
    public void Setup(productInfoData item, SatisfactionManager satisfactionManager)
    {
        associatedItem = item;
        manager = satisfactionManager;

        // Assign visual data (Sirf related values, no extra symbols)
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemImage != null) itemImage.sprite = item.itemSprite;
        if (priceText != null) priceText.text = item.price.ToString();          // '$' hata diya
        if (satisfactionText != null) satisfactionText.text = item.satisfaction.ToString(); // '+' aur 'Sat' hata diya

        // Setup the delete button listener
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
    }

    private void OnDeleteClicked()
    {
        if (manager != null && associatedItem != null)
        {
            manager.RemoveItemFromCart(associatedItem);
        }
    }
}