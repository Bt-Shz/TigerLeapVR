using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class MahjongAnalyticsManager : MonoBehaviour
{
    public static MahjongAnalyticsManager Instance;
    
    [Header("Analytics Data")]
    [SerializeField] private MahjongGameAnalytics currentGameAnalytics;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Enhanced tracking variables for your requirements
    private Dictionary<GameObject, int> tileIncorrectAttempts = new Dictionary<GameObject, int>();
    private List<float> attemptTimestamps = new List<float>();
    private float lastAttemptTime = 0f;
    private float maxDelayTime = 0f;
    private int consecutiveCorrectFromStart = 0;
    private bool hasIncorrectAttemptOccurred = false;
    private bool isGameActive = false;
    private float gameStartTime = 0f;
    
    // New tracking variables for enhanced analytics
    private Dictionary<GameObject, List<float>> tileInteractionTimestamps = new Dictionary<GameObject, List<float>>();
    private int totalCorrectAttempts = 0;
    private int totalIncorrectAttempts = 0;
    
    // NEW: Set completion timing tracking
    private Dictionary<int, float> setStartTimes = new Dictionary<int, float>(); // cardTypeId -> start time
    private Dictionary<int, bool> setCompleted = new Dictionary<int, bool>();    // cardTypeId -> completed status
    private int completedSetsCount = 0;
    
    // Events for other systems to listen to
    public static event Action<MahjongGameAnalytics> OnAnalyticsUpdated;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeNewGame();
    }
    
    /// <summary>
    /// Initialize analytics for a new game
    /// </summary>
    public void InitializeNewGame()
    {
        currentGameAnalytics = new MahjongGameAnalytics();
        tileIncorrectAttempts.Clear();
        tileInteractionTimestamps.Clear();
        attemptTimestamps.Clear();
        lastAttemptTime = 0f;
        maxDelayTime = 0f;
        consecutiveCorrectFromStart = 0;
        hasIncorrectAttemptOccurred = false;
        isGameActive = false;
        gameStartTime = 0f;
        totalCorrectAttempts = 0;
        totalIncorrectAttempts = 0;
        
        // NEW: Reset set completion tracking
        setStartTimes.Clear();
        setCompleted.Clear();
        completedSetsCount = 0;
        
        if (showDebugLogs)
            Debug.Log("🔄 MahjongAnalyticsManager: New game analytics initialized");
    }
    
    /// <summary>
    /// Call this when the game actually starts (after countdown)
    /// </summary>
    public void StartGameTracking(string difficulty)
    {
        isGameActive = true;
        gameStartTime = Time.time;
        lastAttemptTime = gameStartTime;
        currentGameAnalytics.GameDifficulty = difficulty;
        
        if (showDebugLogs)
            Debug.Log($"🎮 MahjongAnalyticsManager: Game tracking started with difficulty: {difficulty}");
    }
    
    /// <summary>
    /// Track a card interaction (before knowing if it's correct or incorrect)
    /// Call this from CardController.OnMouseDown()
    /// </summary>
    public void TrackCardInteraction(GameObject selectedTile)
    {
        if (!isGameActive) return;
        
        float currentTime = Time.time;
        attemptTimestamps.Add(currentTime);
        
        // Track interaction timestamp for this specific tile
        if (!tileInteractionTimestamps.ContainsKey(selectedTile))
        {
            tileInteractionTimestamps[selectedTile] = new List<float>();
        }
        tileInteractionTimestamps[selectedTile].Add(currentTime);
        
        // Calculate delay between attempts
        TrackDelayBetweenAttempts();
        
        if (showDebugLogs)
            Debug.Log($"🎯 Card interaction tracked at time: {currentTime:F2}s");
    }
    
    /// <summary>
    /// Track a correct tile selection
    /// </summary>
    public void TrackCorrectAttempt(GameObject selectedTile)
    {
        if (!isGameActive) return;
        
        currentGameAnalytics.TotalNoofAttempts++;
        totalCorrectAttempts++;
        
        // Track consecutive correct attempts from start
        if (!hasIncorrectAttemptOccurred)
        {
            consecutiveCorrectFromStart++;
            currentGameAnalytics.RememberedCardsSelectedOnStart = consecutiveCorrectFromStart;
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ Correct attempt tracked. Total attempts: {currentGameAnalytics.TotalNoofAttempts}, " +
                     $"Consecutive from start: {currentGameAnalytics.RememberedCardsSelectedOnStart}");
        
        OnAnalyticsUpdated?.Invoke(currentGameAnalytics);
    }
    
    /// <summary>
    /// Track an incorrect tile selection
    /// </summary>
    public void TrackIncorrectAttempt(GameObject selectedTile)
    {
        if (!isGameActive) return;
        
        currentGameAnalytics.TotalNoofAttempts++;
        totalIncorrectAttempts++;
        hasIncorrectAttemptOccurred = true;
        
        // Track incorrect attempts per tile
        if (selectedTile != null)
        {
            if (!tileIncorrectAttempts.ContainsKey(selectedTile))
            {
                tileIncorrectAttempts[selectedTile] = 0;
            }
            
            tileIncorrectAttempts[selectedTile]++;
            
            // Update max incorrect attempts for any single tile
            int maxIncorrectForThisTile = tileIncorrectAttempts[selectedTile];
            if (maxIncorrectForThisTile > currentGameAnalytics.MaxNoofIncorrectAttempts)
            {
                currentGameAnalytics.MaxNoofIncorrectAttempts = maxIncorrectForThisTile;
                
                if (showDebugLogs)
                    Debug.Log($"🔥 New max incorrect attempts on single tile: {maxIncorrectForThisTile}");
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"❌ Incorrect attempt tracked. Total attempts: {currentGameAnalytics.TotalNoofAttempts}, " +
                     $"Max incorrect on tile: {currentGameAnalytics.MaxNoofIncorrectAttempts}");
        
        OnAnalyticsUpdated?.Invoke(currentGameAnalytics);
    }
    
    /// <summary>
    /// Track the delay between tile selections
    /// </summary>
    private void TrackDelayBetweenAttempts()
    {
        float currentTime = Time.time;
        
        if (lastAttemptTime > 0) // Not the first attempt
        {
            float delayTime = currentTime - lastAttemptTime;
            
            if (delayTime > maxDelayTime)
            {
                maxDelayTime = delayTime;
                currentGameAnalytics.MaxDelayBetweenAttempts = maxDelayTime;
                
                if (showDebugLogs)
                    Debug.Log($"⏱️ New max delay recorded: {maxDelayTime:F2} seconds");
            }
        }
        
        lastAttemptTime = currentTime;
    }
    
    /// <summary>
    /// Track when a card type first gets a match (start tracking set completion time)
    /// Call this when the first tile of a card type is correctly matched
    /// </summary>
    public void TrackSetStarted(int cardTypeId)
    {
        if (!isGameActive) return;
        
        if (!setStartTimes.ContainsKey(cardTypeId))
        {
            setStartTimes[cardTypeId] = Time.time;
            setCompleted[cardTypeId] = false;
            
            if (showDebugLogs)
                Debug.Log($"🎯 Set tracking started for card type {cardTypeId} at time: {Time.time:F2}s");
        }
    }
    
    /// <summary>
    /// Track when a set is completed (all 4 tiles of a card type found)
    /// Call this when the 4th tile of a card type is matched
    /// </summary>
    public void TrackSetCompleted(int cardTypeId)
    {
        if (!isGameActive) return;
        
        if (setStartTimes.ContainsKey(cardTypeId) && !setCompleted[cardTypeId])
        {
            float completionTime = Time.time;
            float setDuration = completionTime - setStartTimes[cardTypeId];
            
            setCompleted[cardTypeId] = true;
            completedSetsCount++;
            
            // Store the completion time based on which set this is (1st, 2nd, or 3rd completed)
            switch (completedSetsCount)
            {
                case 1:
                    currentGameAnalytics.TimeTakenForSetFirst = setDuration;
                    if (showDebugLogs)
                        Debug.Log($"🏆 First set completed! Card type {cardTypeId} took {setDuration:F2}s");
                    break;
                case 2:
                    currentGameAnalytics.TimeTakenForSetSecond = setDuration;
                    if (showDebugLogs)
                        Debug.Log($"🏆 Second set completed! Card type {cardTypeId} took {setDuration:F2}s");
                    break;
                case 3:
                    currentGameAnalytics.TimeTakenForSetThird = setDuration;
                    if (showDebugLogs)
                        Debug.Log($"🏆 Third set completed! Card type {cardTypeId} took {setDuration:F2}s - All sets done!");
                    break;
            }
            
            OnAnalyticsUpdated?.Invoke(currentGameAnalytics);
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"⚠️ Attempted to complete set for card type {cardTypeId} but no start time recorded or already completed");
        }
    }
    
    /// <summary>
    /// Calculate comprehensive analytics based on collected data
    /// </summary>
    private void CalculateFinalAnalytics()
    {
        // Recalculate total attempts from our tracking
        currentGameAnalytics.TotalNoofAttempts = totalCorrectAttempts + totalIncorrectAttempts;
        
        // Find the tile with most incorrect attempts
        int maxIncorrectOnSingleTile = 0;
        GameObject mostProblematicTile = null;
        
        foreach (var kvp in tileIncorrectAttempts)
        {
            if (kvp.Value > maxIncorrectOnSingleTile)
            {
                maxIncorrectOnSingleTile = kvp.Value;
                mostProblematicTile = kvp.Key;
            }
        }
        
        currentGameAnalytics.MaxNoofIncorrectAttempts = maxIncorrectOnSingleTile;
        
        // Calculate max delay between attempts from our timestamp data
        float calculatedMaxDelay = 0f;
        for (int i = 1; i < attemptTimestamps.Count; i++)
        {
            float delay = attemptTimestamps[i] - attemptTimestamps[i - 1];
            if (delay > calculatedMaxDelay)
            {
                calculatedMaxDelay = delay;
            }
        }
        
        currentGameAnalytics.MaxDelayBetweenAttempts = calculatedMaxDelay;
        
        if (showDebugLogs)
        {
            Debug.Log($"📊 Final Analytics Calculated:");
            Debug.Log($"   Total Attempts: {currentGameAnalytics.TotalNoofAttempts}");
            Debug.Log($"   Correct Attempts: {totalCorrectAttempts}");
            Debug.Log($"   Incorrect Attempts: {totalIncorrectAttempts}");
            Debug.Log($"   Max Incorrect on Single Tile: {currentGameAnalytics.MaxNoofIncorrectAttempts}");
            Debug.Log($"   Max Delay Between Attempts: {currentGameAnalytics.MaxDelayBetweenAttempts:F2}s");
            Debug.Log($"   Remembered Cards from Start: {currentGameAnalytics.RememberedCardsSelectedOnStart}");
            Debug.Log($"   Set 1 Completion Time: {currentGameAnalytics.TimeTakenForSetFirst:F2}s");
            Debug.Log($"   Set 2 Completion Time: {currentGameAnalytics.TimeTakenForSetSecond:F2}s");
            Debug.Log($"   Set 3 Completion Time: {currentGameAnalytics.TimeTakenForSetThird:F2}s");
            if (mostProblematicTile != null)
                Debug.Log($"   Most Problematic Tile: {mostProblematicTile.name}");
        }
    }
    
    /// <summary>
    /// Call this when the game ends (win or lose)
    /// </summary>
    public void EndGameTracking(bool gameCompleted)
    {
        if (!isGameActive) return;
        
        isGameActive = false;
        currentGameAnalytics.GameCompleted = gameCompleted;
        currentGameAnalytics.GameEndTime = DateTime.Now;
        
        // Calculate final analytics from all collected data
        CalculateFinalAnalytics();
        
        if (showDebugLogs)
        {
            Debug.Log($"🏁 Game ended. Final Analytics Summary:");
            Debug.Log($"   Game Completed: {gameCompleted}");
            Debug.Log($"   Difficulty: {currentGameAnalytics.GameDifficulty}");
            Debug.Log($"   Total Attempts: {currentGameAnalytics.TotalNoofAttempts}");
            Debug.Log($"   Max Incorrect on Single Tile: {currentGameAnalytics.MaxNoofIncorrectAttempts}");
            Debug.Log($"   Max Delay Between Attempts: {currentGameAnalytics.MaxDelayBetweenAttempts:F2}s");
            Debug.Log($"   Remembered Cards from Start: {currentGameAnalytics.RememberedCardsSelectedOnStart}");
            Debug.Log($"   Set 1 Completion Time: {currentGameAnalytics.TimeTakenForSetFirst:F2}s");
            Debug.Log($"   Set 2 Completion Time: {currentGameAnalytics.TimeTakenForSetSecond:F2}s");
            Debug.Log($"   Set 3 Completion Time: {currentGameAnalytics.TimeTakenForSetThird:F2}s");
        }
        
        // Send analytics to Firebase
        StartCoroutine(SendAnalyticsToFirebase());
        
        OnAnalyticsUpdated?.Invoke(currentGameAnalytics);
    }
    
    /// <summary>
    /// Send analytics data to Firebase
    /// </summary>
    private IEnumerator SendAnalyticsToFirebase()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.isFirebaseInitialized)
        {
            Debug.LogError("Firebase not initialized, cannot send analytics");
            yield break;
        }
        
        // Get current user
        var currentUser = FirebaseManager.Instance.GetCurrentUser();
        if (currentUser == null)
        {
            Debug.LogError("No user logged in, cannot send analytics");
            yield break;
        }
        
        // Ensure GM1 (Mahjong) is selected
        if (FirebaseManager.Instance.GetCurrentGameSelection() != "GM1")
        {
            FirebaseManager.Instance.SelectMahjongGame();
        }
        
        yield return new WaitForSeconds(0.1f); // Small delay to ensure selection is set
        
        // Send analytics data
        yield return FirebaseManager.Instance.UpdateMahjongAnalytics(currentGameAnalytics);
        
        Debug.Log("Analytics successfully sent to Firebase");
    }
    
    /// <summary>
    /// Get current analytics data (for debugging or UI display)
    /// </summary>
    public MahjongGameAnalytics GetCurrentAnalytics()
    {
        return currentGameAnalytics;
    }
    
    /// <summary>
    /// Reset analytics for a new game
    /// </summary>
    public void ResetAnalytics()
    {
        InitializeNewGame();
    }
    
    /// <summary>
    /// Get detailed tile-by-tile incorrect attempt data (for debugging)
    /// </summary>
    public Dictionary<GameObject, int> GetTileIncorrectAttempts()
    {
        return new Dictionary<GameObject, int>(tileIncorrectAttempts);
    }
    
    /// <summary>
    /// Manual tracking method for special cases
    /// </summary>
    public void ManuallyUpdateAnalytics(int totalAttempts, int maxIncorrectOnTile, float maxDelay, int rememberedFromStart)
    {
        currentGameAnalytics.TotalNoofAttempts = totalAttempts;
        currentGameAnalytics.MaxNoofIncorrectAttempts = maxIncorrectOnTile;
        currentGameAnalytics.MaxDelayBetweenAttempts = maxDelay;
        currentGameAnalytics.RememberedCardsSelectedOnStart = rememberedFromStart;
        
        OnAnalyticsUpdated?.Invoke(currentGameAnalytics);
    }
    
    /// <summary>
    /// Get performance metrics for the current game
    /// </summary>
    public GamePerformanceMetrics GetPerformanceMetrics()
    {
        float accuracy = currentGameAnalytics.TotalNoofAttempts > 0 ? 
            (float)totalCorrectAttempts / currentGameAnalytics.TotalNoofAttempts * 100f : 0f;
            
        return new GamePerformanceMetrics
        {
            TotalAttempts = currentGameAnalytics.TotalNoofAttempts,
            CorrectAttempts = totalCorrectAttempts,
            IncorrectAttempts = totalIncorrectAttempts,
            Accuracy = accuracy,
            MaxHesitationTime = currentGameAnalytics.MaxDelayBetweenAttempts,
            RememberedFromStart = currentGameAnalytics.RememberedCardsSelectedOnStart,
            MostProblematicTileAttempts = currentGameAnalytics.MaxNoofIncorrectAttempts
        };
    }
    
    /// <summary>
    /// Check if the player is struggling (for adaptive difficulty)
    /// </summary>
    public bool IsPlayerStruggling()
    {
        return currentGameAnalytics.MaxNoofIncorrectAttempts >= 3 || 
               currentGameAnalytics.MaxDelayBetweenAttempts > 10f ||
               (currentGameAnalytics.TotalNoofAttempts > 10 && 
                (float)totalCorrectAttempts / currentGameAnalytics.TotalNoofAttempts < 0.5f);
    }
    
    /// <summary>
    /// Get a detailed tile analysis report
    /// </summary>
    public TileAnalysisReport GetTileAnalysisReport()
    {
        var report = new TileAnalysisReport();
        
        foreach (var kvp in tileIncorrectAttempts)
        {
            var tileData = new TileAnalysisData
            {
                TileObject = kvp.Key,
                IncorrectAttempts = kvp.Value,
                InteractionTimestamps = tileInteractionTimestamps.ContainsKey(kvp.Key) ? 
                    new List<float>(tileInteractionTimestamps[kvp.Key]) : new List<float>()
            };
            
            report.TileAnalytics.Add(tileData);
        }
        
        return report;
    }
}

// Additional data structures for enhanced analytics
[System.Serializable]
public class GamePerformanceMetrics
{
    public int TotalAttempts;
    public int CorrectAttempts;
    public int IncorrectAttempts;
    public float Accuracy;
    public float MaxHesitationTime;
    public int RememberedFromStart;
    public int MostProblematicTileAttempts;
}

[System.Serializable]
public class TileAnalysisData
{
    public GameObject TileObject;
    public int IncorrectAttempts;
    public List<float> InteractionTimestamps;
}

[System.Serializable]
public class TileAnalysisReport
{
    public List<TileAnalysisData> TileAnalytics = new List<TileAnalysisData>();
}
