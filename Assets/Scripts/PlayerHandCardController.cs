using UnityEngine;

public class PlayerHandCardController : MonoBehaviour
{
    [HideInInspector]
    public int cardTypeId = -1;  // Card type for matching with grid cards
    
    void Start()
    {
        // Face-up position (flat position) - always visible
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        
        // Disable collider if exists to prevent clicking
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
    
    // Method to set card type ID (called from CardSpawner)
    public void SetCardTypeId(int id)
    {
        cardTypeId = id;
    }
}