using UnityEngine;
using UnityEngine.UI;

public class GlobalSoundToggle : MonoBehaviour
{
    [Header("Sound Toggle UI")]
    public Button soundButton;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private Image buttonImage;
    private bool isSoundEnabled = true;

    void Start()
    {
        // Validate required components
        if (!ValidateComponents())
        {
            Debug.LogError($"❌ GlobalSoundToggle on {gameObject.name} is missing required components!");
            enabled = false;
            return;
        }

        // Get button image
        buttonImage = soundButton.GetComponent<Image>();

        // Clear existing listeners to avoid duplicates
        soundButton.onClick.RemoveAllListeners();

        // Setup button listener
        soundButton.onClick.AddListener(ToggleSound);

        // Get initial state from PersistentSoundManager
        UpdateFromPersistentManager();

        Debug.Log($"🔊 GlobalSoundToggle initialized - Sound: {isSoundEnabled}");
    }

    private bool ValidateComponents()
    {
        if (soundButton == null)
        {
            Debug.LogError($"Sound button is not assigned on {gameObject.name}");
            return false;
        }

        if (soundOnSprite == null || soundOffSprite == null)
        {
            Debug.LogError($"Sound sprites are not assigned on {gameObject.name}");
            return false;
        }

        if (soundButton.GetComponent<Image>() == null)
        {
            Debug.LogError($"Sound button doesn't have an Image component on {gameObject.name}");
            return false;
        }

        return true;
    }

    void OnEnable()
    {
        // Only update if components are valid
        if (soundButton != null && soundOnSprite != null && soundOffSprite != null)
        {
            UpdateFromPersistentManager();
        }
    }

    // Update state from PersistentSoundManager
    private void UpdateFromPersistentManager()
    {
        if (PersistentSoundManager.Instance != null)
        {
            isSoundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
            UpdateButtonSprite();
        }
        else
        {
            Debug.LogWarning("⚠️ PersistentSoundManager not found - using default state");
            UpdateButtonSprite();
        }
    }

    public void ToggleSound()
    {
        // Use PersistentSoundManager instead of local logic
        if (PersistentSoundManager.Instance != null)
        {
            PersistentSoundManager.Instance.ToggleGlobalSound();
        }
        else
        {
            Debug.LogError("❌ PersistentSoundManager not found!");
            // Fallback: toggle local state
            isSoundEnabled = !isSoundEnabled;
            UpdateButtonSprite();
        }
    }

    // Called by PersistentSoundManager to update visual state
    public void UpdateVisualState(bool soundEnabled)
    {
        isSoundEnabled = soundEnabled;
        UpdateButtonSprite();
    }

    private void UpdateButtonSprite()
    {
        if (buttonImage != null && soundOnSprite != null && soundOffSprite != null)
        {
            buttonImage.sprite = isSoundEnabled ? soundOnSprite : soundOffSprite;
            Debug.Log($"🖼️ Button sprite updated to: {(isSoundEnabled ? "ON" : "OFF")}");
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (soundButton != null)
        {
            soundButton.onClick.RemoveListener(ToggleSound);
        }
    }
}