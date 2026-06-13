using UnityEngine;

public class FoodItemHolderr : MonoBehaviour
{
    public FoodItemSOo foodData; // Scriptable Object reference

    private Vector3 mOffset;
    private float mZCoord;
    private float fixedYHeight; // Jis height par object spawn hua hai (e.g., Y=1)

    private bool isLocked = false;
    private PlateSlot currentHoveredSlot; // Logic from previous step
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        // Object ki original height save kar lo (taaki drag karte waqt wo upar-neeche na ho)
        fixedYHeight = transform.position.y;
    }

    private void OnMouseDown()
    {
        //Debug.Log("Object Clicked: " + gameObject.name);
        if (isLocked) return;

        // 1. Object ka screen Z-depth nikalna (Camera se kitna door hai)
        mZCoord = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;

        // 2. Offset calculate karna (Taaki mouse object ke center me na jump kare)
        mOffset = gameObject.transform.position - GetMouseAsWorldPoint();
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        // Mouse ke pixel coordinates (x,y)
        Vector3 mousePoint = Input.mousePosition;

        // Z coordinate wahi rakho jo object ka hai
        mousePoint.z = mZCoord;

        // World coordinates me convert karo
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void OnMouseDrag()
    {
        if (isLocked) return;

        // 3. Nayi position calculate karo
        Vector3 newPos = GetMouseAsWorldPoint() + mOffset;

        // 4. IMPORTANT: Y value ko fix kar do (Taaki wo zameen par hi rahe)
        transform.position = new Vector3(newPos.x, fixedYHeight, newPos.z);
    }

    private void OnMouseUp()
    {
        // Dragging band karo
        isLocked = false;

        // 1. Check: Kya hum kisi Plate Slot ke upar hain?
        if (currentHoveredSlot != null)
        {
            // 2. Check: Kya Food ki Category aur Plate ki Category Same hai?
            if (foodData.category == currentHoveredSlot.allowedCategory)
            {
                // --- SUCCESS CASE ---

                Debug.Log("✅ Item Saved: " + foodData.foodName);

                // A. Slider Update karo (GameManager ko call karo)
                FoodeChooseGameManagerr.Instance.AddFoodStats(foodData.oil, foodData.sugar);

                // B. Object ko scene se hata do (Destroy)
                Destroy(gameObject);
            }
            else
            {
                // --- WRONG PLATE CASE ---
                Debug.Log("❌ Wrong Plate! This plate needs: " + currentHoveredSlot.allowedCategory);

                // Wapas apni jagah bhej do
                transform.position = startPosition;
            }
        }
        else
        {
            // --- DROPPED IN EMPTY SPACE ---
            // Hawa mein choda toh wapas bhej do
            transform.position = startPosition;
        }
    }

    // ... OnTriggerEnter aur OnTriggerExit logic same rahega ...
    private void OnTriggerEnter(Collider other)
    {
        PlateSlot slot = other.GetComponent<PlateSlot>();
        if (slot != null) currentHoveredSlot = slot;
    }

    private void OnTriggerExit(Collider other)
    {
        PlateSlot slot = other.GetComponent<PlateSlot>();
        if (slot != null && currentHoveredSlot == slot) currentHoveredSlot = null;
    }
}