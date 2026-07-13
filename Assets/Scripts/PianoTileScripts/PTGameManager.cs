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
    private float sessionStartTime;

    // Trigger timing
    private float lastTriggerTime = -1f;
    private float maxDelayBetweenTriggers = 0f;
    private int maxComboAchieved = 0;

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

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<PtAudioManager>();
    }

    void OnEnable()
    {
        BackendFacade.OnUploadFailed += OnUploadFailed;
    }

    void OnDisable()
    {
        BackendFacade.OnUploadFailed -= OnUploadFailed;
    }

    void Start()
    {
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
        sessionStartTime = Time.time;
        lastTriggerTime = -1f;
        maxDelayBetweenTriggers = 0f;
        maxComboAchieved = 0;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].sprite = fullHeartSprite;
        }
    }

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

    public void UpdateMaxCombo(int currentCombo)
    {
        if (currentCombo > maxComboAchieved)
        {
            maxComboAchieved = currentCombo;
        }
    }

    public void NotifyMiss()
    {
        if (isGameOver) return;

        if (scoreManager != null)
        {
            scoreManager.OnCubeMissed();
        }

        currentMisses++;

        if (currentMisses <= heartImages.Count)
        {
            int heartIndex = currentMisses - 1;
            if (heartImages[heartIndex] != null)
                heartImages[heartIndex].sprite = emptyHeartSprite;
        }

        audioManager.PlaySFX(audioManager.Miss);

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

        if (spawnManager != null) spawnManager.StopSpawning();
        if (gateSpawnManager != null) gateSpawnManager.StopSpawning();

        DestroyRemainingObjects();

        Time.timeScale = 0f;

        if (gameOverUI != null)
        {
            int score = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
            gameOverUI.Show(score, currentMisses, maxDelayBetweenTriggers);
        }

        UploadResult(false);
    }

    private void DestroyRemainingObjects()
    {
        var cubes = GameObject.FindGameObjectsWithTag("MovingCube");
        foreach (var c in cubes) Destroy(c);
        if (gateSpawnManager != null) gateSpawnManager.DestroyAllGates();
    }

    private void UploadResult(bool quitting)
    {
        if (!uploadedThisRun)
        {
            uploadedThisRun = true;

            int cubesCaught = scoreManager != null ? scoreManager.GetTotalCubesCaught() : 0;
            float sessionSeconds = Time.time - sessionStartTime;

            if ((long)cubesCaught + currentMisses == 0)
            {
                Debug.Log("Skipping EasyHand upload because the session has no completed attempts.");
            }
            else if (BackendFacade.Instance != null)
            {
                BackendFacade.Instance.UploadEasyHandSession(cubesCaught, currentMisses, sessionSeconds);
            }
            else
            {
                Debug.LogWarning("Cannot upload EasyHand session - BackendFacade not found");
            }
        }

        if (quitting)
        {
            Time.timeScale = 1f;
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

    public void PlayAgain()
    {
        Time.timeScale = 1f;

        ResetRun();
        if (scoreManager != null) scoreManager.ResetScore();
        if (gameOverUI != null) gameOverUI.Hide();

        if (spawnManager != null) spawnManager.StartSpawning();
    }

    public void QuitToMenu()
    {
        UploadResult(true);
    }

    private void OnUploadFailed(string message)
    {
        Debug.LogWarning($"EasyHand upload failed: {message}");
    }

    private IEnumerator showMissFeedback()
    {
        if (missFeedbackImages == null) yield break;
        missFeedbackImages.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(missFeedbackDuration);
        missFeedbackImages.gameObject.SetActive(false);
    }
}
