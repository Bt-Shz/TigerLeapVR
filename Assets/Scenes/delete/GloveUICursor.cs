using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using StretchSense.OSCBridge;
using System.Collections.Generic;

public class GloveUICursor : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the RectTransform of your Cursor Image here.")]
    public RectTransform cursorRect;
    [Tooltip("The main Canvas containing your UI.")]
    public Canvas parentCanvas;

    [Header("Glove Settings")]
    [Tooltip("1 for Left Hand, 0 for Right Hand.")]
    public int handedness = 1;

    [Header("Movement Settings")]
    [Tooltip("Speed of the cursor in Canvas pixels per second.")]
    public float cursorSpeed = 1500f;
    [Tooltip("Lower is snappier, higher is smoother/floatier.")]
    [Range(0.01f, 0.5f)]
    public float smoothness = 0.05f;

    [Header("Interaction Settings")]
    [Tooltip("Drag your Canvas (which has the GraphicRaycaster) here to enable clicking.")]
    public GraphicRaycaster uiRaycaster;
    [Tooltip("Drag your scene's EventSystem here.")]
    public EventSystem eventSystem;

    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    private bool wasTriggerPressed = false;
    private RectTransform canvasRect; // Cached reference to the Canvas bounds

    void Start()
    {
        if (cursorRect != null && parentCanvas != null)
        {
            targetPosition = cursorRect.anchoredPosition;
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("GloveUICursor: Please assign both the Cursor RectTransform and Parent Canvas!");
        }
    }

    void Update()
    {
        if (HandStateManager.Instance == null || cursorRect == null) return;

        ControllerInput input = HandStateManager.Instance.GetHand(handedness).controller;

        // 1. Move the cursor using the Joystick
        MoveCursor(input.joystick_x, input.joystick_y);

        // 2. Click buttons using the Trigger
        HandleClicking(input.trigger_pressed);
    }

    private void MoveCursor(float joyX, float joyY)
    {
        Vector2 inputDelta = new Vector2(joyX, joyY) * cursorSpeed * Time.deltaTime;
        targetPosition += inputDelta;

        // Keep the target strictly inside the Canvas bounds
        ClampToCanvas();

        cursorRect.anchoredPosition = Vector2.SmoothDamp(
            cursorRect.anchoredPosition,
            targetPosition,
            ref currentVelocity,
            smoothness
        );
    }

    private void ClampToCanvas()
    {
        if (canvasRect == null) return;

        // Get the maximum X and Y limits based on the Canvas size
        // We subtract half the cursor's width/height so it doesn't bleed over the edge
        float maxX = (canvasRect.rect.width / 2f) - (cursorRect.rect.width / 2f);
        float maxY = (canvasRect.rect.height / 2f) - (cursorRect.rect.height / 2f);

        // Clamp the target position to those limits
        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -maxY, maxY);
    }

    private void HandleClicking(int triggerState)
    {
        bool isPressed = triggerState == 1;

        if (isPressed && !wasTriggerPressed)
        {
            SimulateUIClick();
        }

        wasTriggerPressed = isPressed;
    }

    private void SimulateUIClick()
    {
        if (uiRaycaster == null || eventSystem == null) return;

        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = cursorRect.position;

        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(pointerData, results);

        if (results.Count > 0)
        {
            GameObject hitObject = results[0].gameObject;
            ExecuteEvents.Execute(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
        }
    }
}