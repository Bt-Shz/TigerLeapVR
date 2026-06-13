using UnityEngine;
using UnityEngine.UI;

public class AutoButtonSoundSetup : MonoBehaviour
{
    [Header("Auto Setup Settings")]
    public bool setupOnStart = true;
    public bool includeInactiveButtons = false;
    
    void Start()
    {
        if (setupOnStart)
        {
            // Re-enable but check PersistentSoundManager first
            if (PersistentSoundManager.Instance != null)
            {
                SetupAllButtons();
            }
            else
            {
                Debug.Log("⚠️ PersistentSoundManager not found - buttons may not respect sound toggle");
            }
        }
    }
    
    [ContextMenu("Setup All Buttons")]
    public void SetupAllButtons()
    {
        // Find all buttons in the scene
        Button[] buttons = Object.FindObjectsByType<Button>(includeInactiveButtons ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        foreach (Button button in buttons)
        {
            // Check if button already has ButtonSoundHandler
            if (button.GetComponent<ButtonSoundHandler>() == null)
            {
                button.gameObject.AddComponent<ButtonSoundHandler>();
                Debug.Log($"Added ButtonSoundHandler to: {button.name}");
            }
        }
        
        Debug.Log($"Setup complete! Added sound handlers to {buttons.Length} buttons.");
    }
}