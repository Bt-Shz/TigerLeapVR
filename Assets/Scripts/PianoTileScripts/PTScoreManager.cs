using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PTScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Points awarded per cube caught")]
    public int pointsPerCube = 10;
    
    [Tooltip("Bonus points for consecutive catches")]
    public int bonusPoints = 5;
    
    [Tooltip("Maximum consecutive bonus multiplier")]
    public int maxBonusMultiplier = 5;
    
    [Header("UI References")]
    [Tooltip("Text component to display the score")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("Optional: Text component for displaying combo")]
    public TextMeshProUGUI comboText;
    
    [Header("Score Display")]
    [Tooltip("Prefix text before score")]
    public string scorePrefix = "Score: ";
    
    [Tooltip("Format for score display")]
    public string scoreFormat = "000000";
    
    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebug = false;

    PtAudioManager audioManager;
    
    // Score tracking
    private int currentScore = 0;
    private int consecutiveCatches = 0;
    private int totalCubesCaught = 0;
    
    // Singleton pattern for easy access
    public static PTScoreManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<PtAudioManager>();
    }
    
    void Start()
    {
        InitializeScore();
    }
    
    /// <summary>
    /// Initializes the score display
    /// </summary>
    private void InitializeScore()
    {
        UpdateScoreDisplay();
        UpdateComboDisplay();
        
        if (showDebug)
        {
            Debug.Log("PTScoreManager: Score system initialized");
        }
    }
    
    /// <summary>
    /// Called when a cube is caught by a button
    /// </summary>
    public void OnCubeCaught()
    {
        // Increment consecutive catches
        consecutiveCatches++;
        totalCubesCaught++;
        
        // Calculate score with bonus
        int earnedPoints = CalculatePoints();
        currentScore += earnedPoints;
        
        // Update displays
        UpdateScoreDisplay();
        UpdateComboDisplay();
        
        // Notify game manager of combo update
        if (PTGameManager.Instance != null)
        {
            PTGameManager.Instance.UpdateMaxCombo(consecutiveCatches);
        }

        audioManager.PlaySFX(audioManager.Hit);
        if (showDebug)
        {
            Debug.Log($"PTScoreManager: Cube caught! Earned {earnedPoints} points. Total: {currentScore}");
        }
    }
    
    /// <summary>
    /// Called when a cube is missed (reaches destruction point)
    /// </summary>
    public void OnCubeMissed()
    {
        // Reset consecutive catches
        if (consecutiveCatches > 0)
        {
            consecutiveCatches = 0;
            UpdateComboDisplay();

            // audioManager.PlaySFX(audioManager.Miss); // Handled by PTGameManager

            if (showDebug)
            {
                Debug.Log("PTScoreManager: Cube missed! Combo reset.");
            }
        }
    }
    
    /// <summary>
    /// Calculates points based on base points and consecutive bonus
    /// </summary>
    private int CalculatePoints()
    {
        int basePoints = pointsPerCube;
        
        // Calculate bonus based on consecutive catches
        int bonusMultiplier = Mathf.Min(consecutiveCatches - 1, maxBonusMultiplier);
        int bonus = bonusMultiplier * bonusPoints;
        
        return basePoints + bonus;
    }
    
    /// <summary>
    /// Updates the score display
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            if (string.IsNullOrEmpty(scoreFormat))
            {
                scoreText.text = scorePrefix + currentScore.ToString();
            }
            else
            {
                scoreText.text = scorePrefix + currentScore.ToString(scoreFormat);
            }
        }
    }
    
    /// <summary>
    /// Updates the combo display
    /// </summary>
    private void UpdateComboDisplay()
    {
        if (comboText != null)
        {
            if (consecutiveCatches > 1)
            {
                comboText.text = $"Combo x{consecutiveCatches}";
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Resets the score (useful for restarting)
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        consecutiveCatches = 0;
        totalCubesCaught = 0;
        
        UpdateScoreDisplay();
        UpdateComboDisplay();
        
        if (showDebug)
        {
            Debug.Log("PTScoreManager: Score reset");
        }
    }
    
    /// <summary>
    /// Gets the current score
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    /// <summary>
    /// Gets total cubes caught
    /// </summary>
    public int GetTotalCubesCaught()
    {
        return totalCubesCaught;
    }
    
    /// <summary>
    /// Gets current combo count
    /// </summary>
    public int GetCurrentCombo()
    {
        return consecutiveCatches;
    }
    
    /// <summary>
    /// Adds bonus points (for special events)
    /// </summary>
    public void AddBonusPoints(int bonus, string reason = "")
    {
        currentScore += bonus;
        UpdateScoreDisplay();

        audioManager.PlaySFX(audioManager.Scorebonus);

        if (showDebug)
        {
            Debug.Log($"PTScoreManager: Bonus points added: {bonus}. Reason: {reason}");
        }
    }
}