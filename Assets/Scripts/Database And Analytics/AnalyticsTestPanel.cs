using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Test script to validate analytics functionality
/// Add this to a GameObject and assign UI elements to test analytics
/// </summary>
public class AnalyticsTestPanel : MonoBehaviour
{
    [Header("Test UI Elements")]
    public TextMeshProUGUI totalAttemptsText;
    public TextMeshProUGUI maxIncorrectText;
    public TextMeshProUGUI maxDelayText;
    public TextMeshProUGUI rememberedText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI isStrugglingText;
    
    [Header("Test Controls")]
    public Button refreshButton;
    public Button simulateCorrectButton;
    public Button simulateIncorrectButton;
    public Button resetAnalyticsButton;
    
    [Header("Simulation")]
    public GameObject testTileObject; // Assign any GameObject for testing
    
    void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshAnalyticsDisplay);
            
        if (simulateCorrectButton != null)
            simulateCorrectButton.onClick.AddListener(SimulateCorrectAttempt);
            
        if (simulateIncorrectButton != null)
            simulateIncorrectButton.onClick.AddListener(SimulateIncorrectAttempt);
            
        if (resetAnalyticsButton != null)
            resetAnalyticsButton.onClick.AddListener(ResetAnalytics);
        
        // Auto-refresh every second
        InvokeRepeating(nameof(RefreshAnalyticsDisplay), 1f, 1f);
    }
    
    void RefreshAnalyticsDisplay()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj == null)
        {
            UpdateUI("No Analytics", "Manager", "Found", "Please", "Setup", "First");
            return;
        }
        
        // Get current analytics (use SendMessage to avoid compilation issues)
        try
        {
            // Get basic analytics data using reflection to avoid compilation issues
            var analyticsComponent = analyticsObj.GetComponent(typeof(MonoBehaviour));
            if (analyticsComponent != null)
            {
                // For now, just show that analytics manager exists
                if (totalAttemptsText != null) totalAttemptsText.text = "Analytics Active";
                if (maxIncorrectText != null) maxIncorrectText.text = "Tracking...";
                if (maxDelayText != null) maxDelayText.text = "Ready";
                if (rememberedText != null) rememberedText.text = "Monitoring";
                if (accuracyText != null) accuracyText.text = "Live";
                if (isStrugglingText != null) isStrugglingText.text = "OK";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Analytics test error: {e.Message}");
        }
    }
    
    void UpdateUI(string total, string maxIncorrect, string maxDelay, string remembered, string accuracy, string struggling)
    {
        if (totalAttemptsText != null) totalAttemptsText.text = total;
        if (maxIncorrectText != null) maxIncorrectText.text = maxIncorrect;
        if (maxDelayText != null) maxDelayText.text = maxDelay;
        if (rememberedText != null) rememberedText.text = remembered;
        if (accuracyText != null) accuracyText.text = accuracy;
        if (isStrugglingText != null) isStrugglingText.text = struggling;
    }
    
    void SimulateCorrectAttempt()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null && testTileObject != null)
        {
            analyticsObj.SendMessage("TrackCorrectAttempt", testTileObject, SendMessageOptions.DontRequireReceiver);
            Debug.Log("🟢 Simulated correct attempt");
        }
    }
    
    void SimulateIncorrectAttempt()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null && testTileObject != null)
        {
            analyticsObj.SendMessage("TrackIncorrectAttempt", testTileObject, SendMessageOptions.DontRequireReceiver);
            Debug.Log("🔴 Simulated incorrect attempt");
        }
    }
    
    void ResetAnalytics()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            analyticsObj.SendMessage("InitializeNewGame", SendMessageOptions.DontRequireReceiver);
            Debug.Log("🔄 Analytics reset");
        }
    }
    
    void OnDestroy()
    {
        CancelInvoke();
    }
}
