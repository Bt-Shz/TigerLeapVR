using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PTGameManager : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("Allowed misses before losing (lose when misses > allowed)")]
    public int maxAllowedMisses = 5;

    [Header("References")]
    public PTSpawnManager spawnManager;
    public PTGateSpawnManager gateSpawnManager;
    public PTScoreManager scoreManager;
    public PTMenuManager menuManager;
    public PTGameOverUI gameOverUI;

    [Header("Debug")]
    public bool showDebug = false;

    // State
    private int currentMisses = 0;
    private bool isGameOver = false;
    private bool uploadedThisRun = false;

    // Trigger timing
    private float lastTriggerTime = -1f;
    private float maxDelayBetweenTriggers = 0f;
    private int maxComboAchieved = 0; // Track maximum combo during session


    [Header("Lives UI")]
    public List<Image> heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;


    [Header(" Miss Feedback")]
    [Tooltip("UI image that flashes when the player misses a cube")]
    public Image missFeedbackImages;

    [Tooltip("How Long the miss feedback image stays visible")]
    public float missFeedbackDuration = 0.5f;

    PtAudioManager audioManager;
    public static PTGameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        audioManager =GameObject.FindGameObjectWithTag("Audio").GetComponent<PtAudioManager>();
    }

    void Start()
    {
        // Auto-wire common refs if missing
        if (spawnManager == null) spawnManager = PTSpawnManager.Instance ?? FindFirstObjectByType<PTSpawnManager>();
        if (scoreManager == null) scoreManager = PTScoreManager.Instance ?? FindFirstObjectByType<PTScoreManager>();
        if (menuManager == null) menuManager = FindFirstObjectByType<PTMenuManager>();
        if (gateSpawnManager == null) gateSpawnManager = FindFirstObjectByType<PTGateSpawnManager>();
        if (gameOverUI == null) gameOverUI = FindFirstObjectByType<PTGameOverUI>();

        if (gameOverUI != null)
        {
            gameOverUI.Hide();
            gameOverUI.onPlayAgain = PlayAgain;
            gameOverUI.onQuit = QuitToMenu;
        }
    }

    public void ResetRun()
    {
        currentMisses = 0;
        isGameOver = false;
        uploadedThisRun = false;
        lastTriggerTime = -1f;
        maxDelayBetweenTriggers = 0f;
        maxComboAchieved = 0;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].sprite = fullHeartSprite;

        }
        
        // NEW: Start hand data recording when game starts
        if (HandDataRecorder.Instance != null)
        {
            HandDataRecorder.Instance.StartRecording("Taichi");
        }
        else
        {
            Debug.LogWarning("⚠️ HandDataRecorder not found in scene");
        }
    }

    // Call from CubeButton when a user triggers an input
    public void NotifyUserTrigger()
    {
        if (isGameOver) return;
        float now = Time.time;
        if (lastTriggerTime >= 0f)
        {
            float delta = now - lastTriggerTime;
            if (delta > maxDelayBetweenTriggers) maxDelayBetweenTriggers = delta;
        }
        lastTriggerTime = now;
    }
    
    // Update max combo if current combo exceeds it
    public void UpdateMaxCombo(int currentCombo)
    {
        if (currentCombo > maxComboAchieved)
        {
            maxComboAchieved = currentCombo;
        }
    }

    // Call when a cube is missed
    public void NotifyMiss()
    {
        if (isGameOver) return;
        
        // Reset combo in ScoreManager
        if (scoreManager != null)
        {
            scoreManager.OnCubeMissed();
        }

        currentMisses++;

        if (currentMisses <= heartImages.Count)
        {
            int heartIndex = currentMisses - 1; //zero-based index
            if (heartImages[heartIndex] != null)
                heartImages[heartIndex].sprite = emptyHeartSprite;
        }

      
        audioManager.PlaySFX(audioManager.Miss);

        /*
        if (missFeedbackImages != null)
            StartCoroutine(showMissFeedback());
        */

        if (showDebug) Debug.Log($"PTGameManager: Missed {currentMisses}/{maxAllowedMisses}");
        if (currentMisses > maxAllowedMisses)
        {
            audioManager.PlaySFX(audioManager.GameOver);

            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Stop spawning
        if (spawnManager != null) spawnManager.StopSpawning();
        if (gateSpawnManager != null) gateSpawnManager.StopSpawning();

        // Destroy remaining cubes and gates
        DestroyRemainingObjects();

        // Pause world
        Time.timeScale = 0f;
        
        // NEW: Stop hand data recording and send to Firebase with comprehensive game data
        if (HandDataRecorder.Instance != null && HandDataRecorder.Instance.IsRecording())
        {
            var session = HandDataRecorder.Instance.GetCurrentSession();
            if (session != null)
            {
                // Basic game info
                session.GameDifficulty = "Normal"; // Taichi doesn't have difficulty levels
                session.GameCompleted = true; // Game ended
                session.FinalScore = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
                
                // Additional Taichi-specific statistics
                session.TotalAttempts = scoreManager != null ? scoreManager.GetTotalCubesCaught() : 0;
                session.FailedAttempts = currentMisses;
                session.TimeTaken = Time.time; // Total time played
                session.MaxCombo = maxComboAchieved; // Use tracked max combo
                session.MaxDelayBetweenActions = maxDelayBetweenTriggers;
                session.TotalCubesSpawned = scoreManager != null ? scoreManager.GetTotalCubesCaught() + currentMisses : 0;
                
                // Calculate hit accuracy
                int totalCubes = scoreManager != null ? scoreManager.GetTotalCubesCaught() + currentMisses : 0;
                if (totalCubes > 0)
                {
                    int cubesCaught = scoreManager != null ? scoreManager.GetTotalCubesCaught() : 0;
                    session.Accuracy = (cubesCaught / (float)totalCubes) * 100f;
                }
            }
            HandDataRecorder.Instance.StopRecordingAndSend();
        }

        // Show UI
        if (gameOverUI != null)
        {
            int score = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
            gameOverUI.Show(score, currentMisses, maxDelayBetweenTriggers);
        }

        // Update Firebase
        StartCoroutine(UploadResultCoroutine(false));
    }

    private void DestroyRemainingObjects()
    {
        var cubes = GameObject.FindGameObjectsWithTag("MovingCube");
        foreach (var c in cubes) Destroy(c);
        if (gateSpawnManager != null) gateSpawnManager.DestroyAllGates();
    }

    private IEnumerator UploadResultCoroutine(bool quitting)
    {
        if (uploadedThisRun) yield break;
        uploadedThisRun = true;

        int score = scoreManager != null ? scoreManager.GetCurrentScore() : 0;

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.isFirebaseInitialized)
        {
            FirebaseManager.Instance.SelectTaichiGame(); // ensure GM2
            var task = FirebaseManager.Instance.UpdateGM2Score(score, maxDelayBetweenTriggers);
            yield return new WaitUntil(() => task.IsCompleted);
        }

        if (quitting)
        {
            // Unpause before changing scenes
            Time.timeScale = 1f;
            // Prefer PTMenuManager if present, else load scene
            if (menuManager != null)
            {
                menuManager.ShowMainMenu();
            }
            else
            {
                try { SceneManager.LoadScene("MainMenu"); }
                catch { SceneManager.LoadScene(0); }
            }
        }
    }

    // UI hooks
    public void PlayAgain()
    {
        // Unpause
        Time.timeScale = 1f;

        // Reset state and score
        ResetRun();
        if (scoreManager != null) scoreManager.ResetScore();
        if (gameOverUI != null) gameOverUI.Hide();

        // Restart spawning
        if (spawnManager != null) spawnManager.StartSpawning();
        // if (gateSpawnManager != null) gateSpawnManager.StartSpawning();
    }

    public void QuitToMenu()
    {
        StartCoroutine(UploadResultCoroutine(true));
    }

    private IEnumerator showMissFeedback()
    {
        if (missFeedbackImages == null) yield break;
        missFeedbackImages.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(missFeedbackDuration);
        missFeedbackImages.gameObject.SetActive(false);
    }
}
