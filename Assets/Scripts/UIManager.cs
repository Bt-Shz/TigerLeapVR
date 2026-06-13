using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI attemptsText;
    
    void Start()
    {
        // Find GameManager and assign reference
        if (GameManager.Instance != null && attemptsText != null) 
        {
            // Assign this text element to the GameManager
            GameManager.Instance.attemptsText = attemptsText;
        }
        else
        {
            Debug.LogWarning("UIManager: Could not find GameManager or attemptsText is not assigned");
        }
        
        // Initialize the attempts display
        UpdateAttemptsUI(0);
    }
    
    // Method to update attempts UI (can be called directly if needed)
    public void UpdateAttemptsUI(int attempts)
    {
        if (attemptsText != null)
        {
            attemptsText.text = "Attempts: " + attempts;
        }
    }
}
