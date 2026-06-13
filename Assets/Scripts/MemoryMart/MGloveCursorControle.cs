using System.Collections;
using System.Collections.Generic;
using StretchSense.OSCBridge;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MGloveCursorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandGestureCursor handCursor;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private EventSystem eventSystem;

    [Header("Cursor Appearance")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private Color normalCursorColor = Color.white;
    [SerializeField] private Color hoverCursorColor = Color.yellow;
    [SerializeField] private Color clickingCursorColor = Color.red;

    [Header("Glove Settings")]
    [SerializeField] private bool useLeftHand = true;
    [SerializeField] private float clickCooldown = 0.15f;

    [Header("Hover Glow")]
    [SerializeField] private Color hoverGlowColor = Color.yellow;
    [SerializeField] private float hoverGlowDistance = 5f;

    [Header("3D Product Detection")]
    [SerializeField] private LayerMask interactableLayer;

    // ── State ──────────────────────────────────────────────────────────────
    private float lastClickTime = -999f;
    private bool wasGrabbing = false;

    // ── Hover refs ─────────────────────────────────────────────────────────
    private Button currentHoveredButton;
    private productInfoData currentHoveredProduct;

    // ── Cached scene refs ──────────────────────────────────────────────────
    private Outline cursorOutline;
    private Camera mainCam;
    private ProductTooltipManager tooltipManager;
    private SatisfactionManager satisfactionManager;

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();

        if (handCursor == null) handCursor = FindFirstObjectByType<HandGestureCursor>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (graphicRaycaster == null && canvas != null)
            graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        if (eventSystem == null) eventSystem = FindFirstObjectByType<EventSystem>();

        if (cursorImage == null && handCursor != null)
            cursorImage = handCursor.GetCursorImage();

        tooltipManager = FindFirstObjectByType<ProductTooltipManager>();
        satisfactionManager = FindFirstObjectByType<SatisfactionManager>();

        //// ── Clean Conflict Check ──
        //if (tooltipManager != null && tooltipManager.controlMode == ProductTooltipManager.CursorControlMode.PhysicalMouse)
        //{
        //    Debug.LogWarning("[MGloveCursorController] ProductTooltipManager is set to PhysicalMouse mode! " +
        //                     "Switching it to CustomCursor to prevent control conflicts.", tooltipManager);
        //    tooltipManager.controlMode = ProductTooltipManager.CursorControlMode.CustomCursor;
        //}

        if (cursorImage != null)
        {
            cursorImage.color = normalCursorColor;
            cursorOutline = cursorImage.GetComponent<Outline>();
            if (cursorOutline == null)
                cursorOutline = cursorImage.gameObject.AddComponent<Outline>();
            cursorOutline.enabled = false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (handCursor == null || !handCursor.IsHandDetected())
        {
            ResetCursorState();
            return;
        }

        bool isGrabbing = IsGrabPressed();
        Vector2 screenPos = handCursor.GetCursorScreenPosition();

        UpdateButtonHover(screenPos);

        if (currentHoveredButton == null)
            UpdateProductHover(screenPos);
        else
            ClearProductHover();

        if (isGrabbing && !wasGrabbing)
        {
            if (Time.time - lastClickTime >= clickCooldown)
            {
                if (currentHoveredButton != null)
                {
                    ClickButton(currentHoveredButton);
                    lastClickTime = Time.time;
                }
                else if (currentHoveredProduct != null)
                {
                    if (satisfactionManager != null)
                        satisfactionManager.BuyItem(currentHoveredProduct);

                    StartCoroutine(ClickFeedback());
                    lastClickTime = Time.time;
                }
            }
        }

        wasGrabbing = isGrabbing;
    }

    bool IsGrabPressed()
    {
        var hsm = HandStateManager.Instance;
        if (hsm == null) return false;

        int handIndex = useLeftHand ? 1 : 2;
        var hand = hsm.GetHand(handIndex);
        if (hand == null || hand.controller == null) return false;

        var c = hand.controller;
        return (c.grab_pressed != 0) || (c.grab_value >= 0.70f);
    }

    void UpdateButtonHover(Vector2 screenPos)
    {
        currentHoveredButton = null;
        if (graphicRaycaster == null || eventSystem == null) return;

        PointerEventData pointerData = new PointerEventData(eventSystem) { position = screenPos };
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        foreach (var result in results)
        {
            Button btn = result.gameObject.GetComponent<Button>();
            if (btn != null && btn.interactable)
            {
                currentHoveredButton = btn;
                ApplyHoverVisuals();
                return;
            }
        }

        if (currentHoveredProduct == null)
            ResetCursorVisuals();
    }

    void UpdateProductHover(Vector2 screenPos)
    {
        if (mainCam == null) return;
        Ray ray = mainCam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            productInfoData product = hit.collider.GetComponent<productInfoData>();
            if (product != null)
            {
                currentHoveredProduct = product;
                ApplyHoverVisuals();

                if (tooltipManager != null)
                {
                    tooltipManager.ShowTooltip(product, screenPos);
                }
                return;
            }
        }

        ClearProductHover();
    }

    void HideTooltip()
    {
        if (tooltipManager != null)
            tooltipManager.HideTooltip();
    }

    void ClearProductHover()
    {
        if (currentHoveredProduct != null)
        {
            currentHoveredProduct = null;
            HideTooltip();
            if (currentHoveredButton == null)
                ResetCursorVisuals();
        }
    }

    void ClickButton(Button button)
    {
        button.onClick.Invoke();
        StartCoroutine(ClickFeedback());
    }

    void ApplyHoverVisuals()
    {
        if (cursorImage == null) return;
        cursorImage.color = hoverCursorColor;
        if (cursorOutline != null)
        {
            cursorOutline.enabled = true;
            cursorOutline.effectColor = hoverGlowColor;
            cursorOutline.effectDistance = new Vector2(hoverGlowDistance, -hoverGlowDistance);
        }
    }

    void ResetCursorVisuals()
    {
        if (cursorImage == null) return;
        cursorImage.color = normalCursorColor;
        if (cursorOutline != null) cursorOutline.enabled = false;
    }

    void ResetCursorState()
    {
        currentHoveredButton = null;
        currentHoveredProduct = null;
        HideTooltip();
        ResetCursorVisuals();
    }

    private IEnumerator ClickFeedback()
    {
        if (cursorImage == null) yield break;
        cursorImage.color = clickingCursorColor;
        yield return new WaitForSeconds(0.1f);

        if (currentHoveredButton != null || currentHoveredProduct != null)
            cursorImage.color = hoverCursorColor;
        else
            cursorImage.color = normalCursorColor;
    }
}