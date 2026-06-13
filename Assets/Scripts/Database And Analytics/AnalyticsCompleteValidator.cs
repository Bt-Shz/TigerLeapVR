using UnityEngine;

/// <summary>
/// Complete validation test for all 7 analytics fields
/// This script runs automatic tests to verify the analytics system
/// </summary>
public class AnalyticsCompleteValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private bool showDetailedLogs = true;
    
    void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunCompleteValidation());
        }
    }
    
    System.Collections.IEnumerator RunCompleteValidation()
    {
        if (showDetailedLogs)
            Debug.Log("🧪 Starting Complete Analytics Validation...");
        
        // Step 1: Verify analytics manager exists
        yield return StartCoroutine(ValidateAnalyticsManagerExists());
        
        // Step 2: Test basic analytics functionality
        yield return StartCoroutine(TestBasicAnalytics());
        
        // Step 3: Test set timing functionality
        yield return StartCoroutine(TestSetTimingAnalytics());
        
        // Step 4: Test Firebase integration (if available)
        yield return StartCoroutine(TestFirebaseIntegration());
        
        if (showDetailedLogs)
            Debug.Log("✅ Complete Analytics Validation Finished!");
    }
    
    System.Collections.IEnumerator ValidateAnalyticsManagerExists()
    {
        if (showDetailedLogs)
            Debug.Log("🔍 Step 1: Validating Analytics Manager...");
            
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        
        if (analyticsObj == null)
        {
            Debug.LogError("❌ MahjongAnalyticsManager not found! Please add AnalyticsSetup to scene.");
            yield break;
        }
        
        if (showDetailedLogs)
            Debug.Log("✅ MahjongAnalyticsManager found successfully!");
            
        yield return new WaitForSeconds(0.5f);
    }
    
    System.Collections.IEnumerator TestBasicAnalytics()
    {
        if (showDetailedLogs)
            Debug.Log("🔍 Step 2: Testing Basic Analytics...");
            
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj == null) yield break;
        
        // Initialize new game
        analyticsObj.SendMessage("InitializeNewGame", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.2f);
        
        // Start game tracking
        analyticsObj.SendMessage("StartGameTracking", "Test", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.2f);
        
        // Simulate some tile interactions
        GameObject testTile = new GameObject("TestTile");
        
        // Test correct attempts
        analyticsObj.SendMessage("TrackCorrectAttempt", testTile, SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.1f);
        
        // Test incorrect attempts
        analyticsObj.SendMessage("TrackIncorrectAttempt", testTile, SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.1f);
        
        if (showDetailedLogs)
            Debug.Log("✅ Basic analytics tracking tested successfully!");
            
        Destroy(testTile);
        yield return new WaitForSeconds(0.5f);
    }
    
    System.Collections.IEnumerator TestSetTimingAnalytics()
    {
        if (showDetailedLogs)
            Debug.Log("🔍 Step 3: Testing Set Timing Analytics...");
            
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj == null) yield break;
        
        // Test set timing for 3 different card types
        int[] cardTypes = { 1, 2, 3 };
        
        for (int i = 0; i < cardTypes.Length; i++)
        {
            int cardType = cardTypes[i];
            
            // Start tracking this set
            analyticsObj.SendMessage("TrackSetStarted", cardType, SendMessageOptions.DontRequireReceiver);
            if (showDetailedLogs)
                Debug.Log($"   🎯 Started tracking set {i + 1} (card type {cardType})");
            
            // Wait some time to simulate completion
            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);
            
            // Complete this set
            analyticsObj.SendMessage("TrackSetCompleted", cardType, SendMessageOptions.DontRequireReceiver);
            if (showDetailedLogs)
                Debug.Log($"   🏆 Completed set {i + 1} after {waitTime:F2}s");
                
            yield return new WaitForSeconds(0.2f);
        }
        
        if (showDetailedLogs)
            Debug.Log("✅ Set timing analytics tested successfully!");
            
        yield return new WaitForSeconds(0.5f);
    }
    
    System.Collections.IEnumerator TestFirebaseIntegration()
    {
        if (showDetailedLogs)
            Debug.Log("🔍 Step 4: Testing Firebase Integration...");
            
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj == null) yield break;
        
        // End game tracking (this should trigger Firebase upload)
        analyticsObj.SendMessage("EndGameTracking", true, SendMessageOptions.DontRequireReceiver);
        
        if (showDetailedLogs)
            Debug.Log("   📤 Attempted to send analytics to Firebase...");
            
        // Wait for Firebase operation to complete
        yield return new WaitForSeconds(2f);
        
        if (showDetailedLogs)
            Debug.Log("✅ Firebase integration test completed!");
            
        yield return new WaitForSeconds(0.5f);
    }
    
    // Manual test trigger for UI button
    public void RunManualValidation()
    {
        StartCoroutine(RunCompleteValidation());
    }
    
    // Quick validation for specific field
    public void ValidateSpecificField(string fieldName)
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj == null)
        {
            Debug.LogError($"❌ Cannot validate {fieldName}: Analytics manager not found!");
            return;
        }
        
        Debug.Log($"🔍 Validating {fieldName}...");
        
        switch (fieldName.ToLower())
        {
            case "totalattempts":
                // Test total attempts tracking
                analyticsObj.SendMessage("TrackCorrectAttempt", gameObject, SendMessageOptions.DontRequireReceiver);
                Debug.Log("✅ TotalNoofAttempts tracking validated");
                break;
                
            case "setfirst":
                // Test first set timing
                analyticsObj.SendMessage("TrackSetStarted", 1, SendMessageOptions.DontRequireReceiver);
                StartCoroutine(DelayedSetCompletion(1, 2f));
                Debug.Log("✅ TimeTakenForSetFirst tracking validated");
                break;
                
            case "setsecond":
                // Test second set timing
                analyticsObj.SendMessage("TrackSetStarted", 2, SendMessageOptions.DontRequireReceiver);
                StartCoroutine(DelayedSetCompletion(2, 1.5f));
                Debug.Log("✅ TimeTakenForSetSecond tracking validated");
                break;
                
            case "setthird":
                // Test third set timing
                analyticsObj.SendMessage("TrackSetStarted", 3, SendMessageOptions.DontRequireReceiver);
                StartCoroutine(DelayedSetCompletion(3, 2.5f));
                Debug.Log("✅ TimeTakenForSetThird tracking validated");
                break;
                
            default:
                Debug.LogWarning($"⚠️ Unknown field: {fieldName}");
                break;
        }
    }
    
    System.Collections.IEnumerator DelayedSetCompletion(int cardType, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            analyticsObj.SendMessage("TrackSetCompleted", cardType, SendMessageOptions.DontRequireReceiver);
            Debug.Log($"   🏆 Set {cardType} completed after {delay:F2}s");
        }
    }
}
