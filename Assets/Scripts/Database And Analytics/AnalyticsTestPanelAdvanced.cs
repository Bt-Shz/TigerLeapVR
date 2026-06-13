using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhanced test panel to validate all analytics functionality including set timing
/// </summary>
public class AnalyticsTestPanelAdvanced : MonoBehaviour
{
    [Header("Basic Analytics UI")]
    public TextMeshProUGUI totalAttemptsText;
    public TextMeshProUGUI maxIncorrectText;
    public TextMeshProUGUI maxDelayText;
    public TextMeshProUGUI rememberedText;
    
    [Header("NEW: Set Timing Analytics UI")]
    public TextMeshProUGUI setFirstTimeText;
    public TextMeshProUGUI setSecondTimeText;
    public TextMeshProUGUI setThirdTimeText;
    public TextMeshProUGUI completedSetsText;
    
    [Header("Test Controls")]
    public Button refreshButton;
    public Button startGameButton;
    public Button simulateSet1Button;
    public Button simulateSet2Button;
    public Button simulateSet3Button;
    public Button endGameButton;
    public Button resetAnalyticsButton;
    
    [Header("Set Simulation")]
    public int testCardType1 = 1;
    public int testCardType2 = 2;
    public int testCardType3 = 3;
    
    private bool gameRunning = false;
    
    void Start()
    {
        SetupButtons();
        
        // Auto-refresh every second
        InvokeRepeating(nameof(RefreshAnalyticsDisplay), 1f, 1f);
    }
    
    void SetupButtons()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshAnalyticsDisplay);
            
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGameTest);
            
        if (simulateSet1Button != null)
            simulateSet1Button.onClick.AddListener(() => SimulateSetCompletion(testCardType1));
            
        if (simulateSet2Button != null)
            simulateSet2Button.onClick.AddListener(() => SimulateSetCompletion(testCardType2));
            
        if (simulateSet3Button != null)
            simulateSet3Button.onClick.AddListener(() => SimulateSetCompletion(testCardType3));
            
        if (endGameButton != null)
            endGameButton.onClick.AddListener(EndGameTest);
            
        if (resetAnalyticsButton != null)
            resetAnalyticsButton.onClick.AddListener(ResetAnalytics);
    }
    
    void RefreshAnalyticsDisplay()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj == null)
        {
            UpdateBasicUI("No Analytics", "Manager", "Found", "Setup");
            UpdateSetTimingUI("First", "Please", "Setup", "0");
            return;
        }
        
        // Update basic analytics display
        if (totalAttemptsText != null) totalAttemptsText.text = "Active";
        if (maxIncorrectText != null) maxIncorrectText.text = "Tracking";
        if (maxDelayText != null) maxDelayText.text = "Ready";
        if (rememberedText != null) rememberedText.text = "Live";
        
        // Update set timing display (these will show actual values when sets are completed)
        if (setFirstTimeText != null) setFirstTimeText.text = gameRunning ? "Tracking..." : "Not Started";
        if (setSecondTimeText != null) setSecondTimeText.text = gameRunning ? "Tracking..." : "Not Started";
        if (setThirdTimeText != null) setThirdTimeText.text = gameRunning ? "Tracking..." : "Not Started";
        if (completedSetsText != null) completedSetsText.text = gameRunning ? "Game Running" : "Game Stopped";
    }
    
    void UpdateBasicUI(string total, string maxIncorrect, string maxDelay, string remembered)
    {
        if (totalAttemptsText != null) totalAttemptsText.text = total;
        if (maxIncorrectText != null) maxIncorrectText.text = maxIncorrect;
        if (maxDelayText != null) maxDelayText.text = maxDelay;
        if (rememberedText != null) rememberedText.text = remembered;
    }
    
    void UpdateSetTimingUI(string set1, string set2, string set3, string completed)
    {
        if (setFirstTimeText != null) setFirstTimeText.text = set1;
        if (setSecondTimeText != null) setSecondTimeText.text = set2;
        if (setThirdTimeText != null) setThirdTimeText.text = set3;
        if (completedSetsText != null) completedSetsText.text = completed;
    }
    
    void StartGameTest()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            // Initialize new game
            analyticsObj.SendMessage("InitializeNewGame", SendMessageOptions.DontRequireReceiver);
            
            // Start game tracking with Medium difficulty
            analyticsObj.SendMessage("StartGameTracking", "Medium", SendMessageOptions.DontRequireReceiver);
            
            gameRunning = true;
            Debug.Log("🎮 Test game started - ready to track set completions!");
            
            // Update button states
            if (startGameButton != null) startGameButton.interactable = false;
            if (simulateSet1Button != null) simulateSet1Button.interactable = true;
            if (simulateSet2Button != null) simulateSet2Button.interactable = true;
            if (simulateSet3Button != null) simulateSet3Button.interactable = true;
            if (endGameButton != null) endGameButton.interactable = true;
        }
    }
    
    void SimulateSetCompletion(int cardTypeId)
    {
        if (!gameRunning) return;
        
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            // First, track that this set was started (if not already)
            analyticsObj.SendMessage("TrackSetStarted", cardTypeId, SendMessageOptions.DontRequireReceiver);
            
            // Wait a moment to simulate time passing
            StartCoroutine(SimulateSetCompletionWithDelay(cardTypeId));
        }
    }
    
    System.Collections.IEnumerator SimulateSetCompletionWithDelay(int cardTypeId)
    {
        // Simulate some time passing (1-3 seconds)
        float delayTime = Random.Range(1f, 3f);
        yield return new WaitForSeconds(delayTime);
        
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            // Track set completed
            analyticsObj.SendMessage("TrackSetCompleted", cardTypeId, SendMessageOptions.DontRequireReceiver);
            
            Debug.Log($"🏆 Simulated completion of set {cardTypeId} after {delayTime:F2}s");
        }
    }
    
    void EndGameTest()
    {
        if (!gameRunning) return;
        
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            // End game tracking with win = true
            analyticsObj.SendMessage("EndGameTracking", true, SendMessageOptions.DontRequireReceiver);
            
            gameRunning = false;
            Debug.Log("🎯 Test game ended - analytics should be sent to Firebase!");
            
            // Update button states
            if (startGameButton != null) startGameButton.interactable = true;
            if (simulateSet1Button != null) simulateSet1Button.interactable = false;
            if (simulateSet2Button != null) simulateSet2Button.interactable = false;
            if (simulateSet3Button != null) simulateSet3Button.interactable = false;
            if (endGameButton != null) endGameButton.interactable = false;
        }
    }
    
    void ResetAnalytics()
    {
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            analyticsObj.SendMessage("InitializeNewGame", SendMessageOptions.DontRequireReceiver);
            gameRunning = false;
            Debug.Log("🔄 Analytics reset - ready for new test");
            
            // Reset button states
            if (startGameButton != null) startGameButton.interactable = true;
            if (simulateSet1Button != null) simulateSet1Button.interactable = false;
            if (simulateSet2Button != null) simulateSet2Button.interactable = false;
            if (simulateSet3Button != null) simulateSet3Button.interactable = false;
            if (endGameButton != null) endGameButton.interactable = false;
        }
    }
    
    void OnDestroy()
    {
        CancelInvoke();
    }
}
