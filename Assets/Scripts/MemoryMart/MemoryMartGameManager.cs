using UnityEngine;
using TMPro;
using UnityEngine.EventSystems; // --- NEW: Required to detect UI panels! ---

public class ProductTooltipManager : MonoBehaviour
{
    public enum CursorControlMode
    {
        PhysicalMouse,
        CustomCursor    // Selected when using scripts like MGloveCursorController
    }

    [Header("UI Components")]
    [Tooltip("The parent UI Panel that contains the text.")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI satisfactionText;
    public TextMeshProUGUI productNameText;

    [Header("Settings")]
    [Tooltip("Offset so the cursor doesn't cover the UI panel.")]
    public Vector2 cursorOffset = new Vector2(15f, -15f);
    [Tooltip("Layer assigned to your product objects to optimize raycasting.")]
    public LayerMask interactableLayer;

    [Header("Conflict Management")]
    [Tooltip("Select how this tooltip position and visibility are driven.")]
    public CursorControlMode controlMode = CursorControlMode.PhysicalMouse;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Only run internal raycasting if set to standard hardware mouse
        if (controlMode == CursorControlMode.PhysicalMouse)
        {
            CheckForProductHover();

            if (tooltipPanel.activeSelf)
            {
                UpdateTooltipPosition(Input.mousePosition);
            }
        }
    }

    private void CheckForProductHover()
    {
        // --- NEW: Check if the mouse is hovering over ANY UI Panel! ---
        if (EventSystem.current.IsPointerOverGameObject())
        {
            HideTooltip(); // Hide the tooltip if we drag the mouse onto the UI
            return;        // Stop the code here so the raycast never happens!
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            productInfoData productData = hit.collider.GetComponent<productInfoData>();

            if (productData != null)
            {
                ShowTooltip(productData, Input.mousePosition);

                if (Input.GetMouseButtonDown(0))
                {
                    SatisfactionManager manager = Object.FindFirstObjectByType<SatisfactionManager>();
                    if (manager != null)
                    {
                        manager.BuyItem(productData);
                    }
                }
                return;
            }
        }
        HideTooltip();
    }

    // Public method so external scripts (like the glove) can display data cleanly
    public void ShowTooltip(productInfoData productData, Vector2 screenPosition)
    {
        if (productNameText != null) productNameText.text = productData.itemName;
        if (costText != null) costText.text = productData.price.ToString();
        if (satisfactionText != null) satisfactionText.text = productData.satisfaction.ToString();

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
            UpdateTooltipPosition(screenPosition);
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void UpdateTooltipPosition(Vector2 screenPosition)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.transform.position = (Vector3)screenPosition + (Vector3)cursorOffset;
        }
    }
}