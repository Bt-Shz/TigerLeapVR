using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using StretchSense.OSCBridge;
using System.Collections.Generic;
using System.Collections;

public class GloveCursorController : MonoBehaviour
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

    [Header("2D Food Drag Settings")]
    [Tooltip("Assign the Layer your Food items are on")]
    [SerializeField] private LayerMask foodLayerMask;

    // State Variables
    private float lastClickTime = -999f;
    private bool wasGrabbing = false;

    // Hover References
    private Button currentHoveredButton;
    private FoodItemHolder currentHoveredFood;

    // Drag and Drop References
    private FoodItemHolder grabbedFood;

    // Visuals
    private Outline cursorOutline;

    void Start()
    {
        if (handCursor == null) handCursor = FindFirstObjectByType<HandGestureCursor>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (graphicRaycaster == null && canvas != null) graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        if (eventSystem == null) eventSystem = FindFirstObjectByType<EventSystem>();

        if (cursorImage == null && handCursor != null) cursorImage = handCursor.GetCursorImage();

        if (cursorImage != null)
        {
            cursorImage.color = normalCursorColor;
            cursorOutline = cursorImage.GetComponent<Outline>();
            if (cursorOutline == null) cursorOutline = cursorImage.gameObject.AddComponent<Outline>();
            cursorOutline.enabled = false;
        }
    }

    void Update()
    {
        if (handCursor == null || !handCursor.IsHandDetected())
        {
            ResetCursorState();
            return;
        }

        bool isGrabbing = IsGrabPressed();
        Vector2 screenPos = handCursor.GetCursorScreenPosition();

        // 1. Process Hover logic (Only if we aren't currently dragging something)
        if (grabbedFood == null)
        {
            UpdateCursorHover(screenPos); // Check UI Buttons

            if (currentHoveredButton == null)
                UpdateFoodHover(screenPos); // Check 2D Food if no UI is hovered
            else
                ClearFoodHover();
        }

        // 2. Handle Grab / Click transitions
        if (isGrabbing && !wasGrabbing)
        {
            // Just Pressed Grab
            if (Time.time - lastClickTime >= clickCooldown)
            {
                if (currentHoveredButton != null)
                {
                    ClickButton(currentHoveredButton);
                    lastClickTime = Time.time;
                }
                else if (currentHoveredFood != null)
                {
                    GrabFood(currentHoveredFood, screenPos);
                    lastClickTime = Time.time;
                }
            }
        }
        else if (!isGrabbing && wasGrabbing)
        {
            // Just Released Grab (Idle)
            if (grabbedFood != null)
            {
                DropFood();
            }
        }

        // 3. Handle Dragging Position Updates
        if (grabbedFood != null)
        {
            ProcessDragging(screenPos);
        }

        // Update previous state for next frame
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

    void UpdateCursorHover(Vector2 screenPos)
    {
        currentHoveredButton = null;

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

        if (currentHoveredFood == null) ResetCursorState();
    }

    void UpdateFoodHover(Vector2 screenPos)
    {
        // Convert screen position to 2D world point
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 point2D = new Vector2(worldPoint.x, worldPoint.y);

        // Check for 2D colliders at the cursor's position
        Collider2D hitCollider = Physics2D.OverlapPoint(point2D, foodLayerMask);

        if (hitCollider != null)
        {
            FoodItemHolder hitFood = hitCollider.GetComponent<FoodItemHolder>();
            if (hitFood != null)
            {
                currentHoveredFood = hitFood;
                ApplyHoverVisuals();
                return;
            }
        }

        ClearFoodHover();
    }

    void ClearFoodHover()
    {
        if (currentHoveredFood != null)
        {
            currentHoveredFood = null;
            if (currentHoveredButton == null) ResetCursorState();
        }
    }

    void GrabFood(FoodItemHolder food, Vector2 screenPos)
    {
        grabbedFood = food;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
        worldPos.z = 0f;

        // Tell the food item it is being grabbed by the glove
        grabbedFood.GloveGrab(worldPos);

        if (cursorImage != null) cursorImage.color = clickingCursorColor;
    }

    void ProcessDragging(Vector2 screenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
        worldPos.z = 0f;

        // Tell the food item to move to the cursor
        grabbedFood.GloveDrag(worldPos);
    }

    void DropFood()
    {
        // Tell the food item it has been released
        grabbedFood.GloveDrop();
        grabbedFood = null;

        ClearFoodHover();
        ResetCursorState();
    }

    void ClickButton(Button button)
    {
        button.onClick.Invoke();
        StartCoroutine(ClickFeedback());
    }

    private void ApplyHoverVisuals()
    {
        if (cursorImage != null && grabbedFood == null)
        {
            cursorImage.color = hoverCursorColor;
            if (cursorOutline != null)
            {
                cursorOutline.enabled = true;
                cursorOutline.effectColor = hoverGlowColor;
                cursorOutline.effectDistance = new Vector2(hoverGlowDistance, -hoverGlowDistance);
            }
        }
    }

    void ResetCursorState()
    {
        if (grabbedFood != null) return;
        currentHoveredButton = null;
        currentHoveredFood = null;

        if (cursorImage != null)
        {
            cursorImage.color = normalCursorColor;
            if (cursorOutline != null) cursorOutline.enabled = false;
        }
    }

    private IEnumerator ClickFeedback()
    {
        if (cursorImage != null)
        {
            cursorImage.color = clickingCursorColor;
            yield return new WaitForSeconds(0.1f);

            if (grabbedFood != null) cursorImage.color = clickingCursorColor;
            else if (currentHoveredButton != null || currentHoveredFood != null) cursorImage.color = hoverCursorColor;
            else cursorImage.color = normalCursorColor;
        }
    }
}