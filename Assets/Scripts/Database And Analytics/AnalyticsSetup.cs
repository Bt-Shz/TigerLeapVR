using UnityEngine;

/// <summary>
/// This script ensures MahjongAnalyticsManager is properly set up in the scene
/// Add this to a GameObject in your Mahjong game scene
/// </summary>
public class AnalyticsSetup : MonoBehaviour
{
    [Header("Analytics Manager Setup")]
    [SerializeField] private bool autoCreateAnalyticsManager = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    void Awake()
    {
        SetupAnalyticsManager();
    }
    
    void SetupAnalyticsManager()
    {
        // Check if MahjongAnalyticsManager already exists in the scene
        GameObject existingAnalytics = GameObject.Find("MahjongAnalyticsManager");
        
        if (existingAnalytics == null && autoCreateAnalyticsManager)
        {
            // Create a new GameObject for analytics
            GameObject analyticsObj = new GameObject("MahjongAnalyticsManager");
            
            // Add the MahjongAnalyticsManager component
            var analyticsManager = analyticsObj.AddComponent<MahjongAnalyticsManager>();
            
            // Don't destroy on load to persist between scenes
            DontDestroyOnLoad(analyticsObj);
            
            Debug.Log("✅ MahjongAnalyticsManager created and set up successfully!");
        }
        else if (existingAnalytics != null)
        {
            Debug.Log("✅ MahjongAnalyticsManager already exists in scene.");
        }
        else
        {
            Debug.LogWarning("⚠️ Auto-create analytics manager is disabled. Please manually add MahjongAnalyticsManager to the scene.");
        }
    }
    
    void Start()
    {
        // Initialize analytics for new game when scene starts
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            analyticsObj.SendMessage("InitializeNewGame", SendMessageOptions.DontRequireReceiver);
            
            if (enableDebugLogs)
                Debug.Log("🔄 Analytics initialized for new game session");
        }
    }
}
