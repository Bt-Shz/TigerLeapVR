using UnityEngine;

public class FoodItemHolder : MonoBehaviour
{
    public FoodItemSO foodData;

    private Vector3 offset;
    private bool isLocked = false;
    private bool isDragging = false;
    private PlateSlot2d currentHoveredSlot;
    private Vector3 startPosition;

    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer) originalSortingOrder = spriteRenderer.sortingOrder;
    }

    // 🔥 NEW: Har frame me food aage move karega
    void Update()
    {
        // Agar time ruka hai, ya drag ho raha hai, toh move mat karo
        if (Time.timeScale == 0f || isLocked || isDragging) return;

        // Last spawn point dhundo
        Transform endPoint = FoodSpawner.Instance.spawnPoints[FoodSpawner.Instance.spawnPoints.Length - 1];

        // End point ki taraf move karo
        transform.position = Vector3.MoveTowards(transform.position, endPoint.position, FoodSpawner.Instance.moveSpeed * Time.deltaTime);

        // Agar last point par pahunch gaya, toh wapas pool me daal do (invisible karke)
        if (Vector3.Distance(transform.position, endPoint.position) < 0.1f)
        {
            FoodSpawner.Instance.ReturnToPool(gameObject);
        }
    }

    // ==========================================
    // GLOVE INPUT HOOKS (Called by HandCursorClickHandler)
    // ==========================================

    public void GloveGrab(Vector3 cursorWorldPos)
    {
        if (Time.timeScale == 0f || isLocked) return;

        isDragging = true;

        // Grab karte time ki position save kar lo
        startPosition = transform.position;

        if (spriteRenderer) spriteRenderer.sortingOrder = 100;

        offset = transform.position - cursorWorldPos;

        if (currentHoveredSlot != null)
        {
            bool isCorrect = (foodData.category == currentHoveredSlot.allowedCategory);
            currentHoveredSlot.SetHoverVisual(isCorrect);
        }
    }

    public void GloveDrag(Vector3 cursorWorldPos)
    {
        if (Time.timeScale == 0f || isLocked || !isDragging) return;
        transform.position = cursorWorldPos + offset;
    }

    public void GloveDrop()
    {
        if (Time.timeScale == 0f || !isDragging) return;

        isDragging = false;
        isLocked = false;

        AudioManager.Instance.Play("Pickup");
        if (spriteRenderer) spriteRenderer.sortingOrder = originalSortingOrder;

        // 🔥 FIX: If trigger exit already cleared it, do a live physics check at drop position
        if (currentHoveredSlot == null)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
            foreach (var hit in hits)
            {
                PlateSlot2d found = hit.GetComponent<PlateSlot2d>();
                if (found != null)
                {
                    currentHoveredSlot = found;
                    break;
                }
            }
        }

        if (currentHoveredSlot != null)
        {
            currentHoveredSlot.ResetVisual();

            if (foodData.category == currentHoveredSlot.allowedCategory)
            {
                if (currentHoveredSlot.HasSpace())
                {
                    currentHoveredSlot.ReceiveFood(foodData);
                    FoodSpawner.Instance.ReturnToPool(gameObject);
                }
                else
                {
                    FoodSpawner.Instance.ReturnToPool(gameObject);
                }
            }
            else
            {
                FoodeChooseGameManager.Instance.HandleWrongPlacement(currentHoveredSlot.transform.position);
                FoodSpawner.Instance.ReturnToPool(gameObject);
            }

            currentHoveredSlot = null; // 🔥 Clean up after use
        }
        else
        {
            FoodSpawner.Instance.ReturnToPool(gameObject);
        }
    }

    // ==========================================
    // STANDARD MOUSE INPUTS (For Editor Testing)
    // ==========================================

    private void OnMouseDown()
    {
        GloveGrab(GetMouseWorldPos());
    }

    private void OnMouseDrag()
    {
        GloveDrag(GetMouseWorldPos());
    }

    private void OnMouseUp()
    {
        GloveDrop();
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePoint);
        worldPos.z = 0f;
        return worldPos;
    }

    // ==========================================
    // PLATE TRIGGER LOGIC
    // ==========================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlateSlot2d slot = other.GetComponent<PlateSlot2d>();
        if (slot != null)
        {
            currentHoveredSlot = slot;

            if (isDragging)
            {
                bool isCorrect = (foodData.category == currentHoveredSlot.allowedCategory);
                currentHoveredSlot.SetHoverVisual(isCorrect);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlateSlot2d slot = other.GetComponent<PlateSlot2d>();
        if (slot != null)
        {
            slot.ResetVisual();

            if (currentHoveredSlot == slot)
            {
                currentHoveredSlot = null;
            }
        }
    }
}