using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FoodeChooseGameManager : MonoBehaviour
{
    public static FoodeChooseGameManager Instance;

    [Header("UI Text References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;

    [Header("Live Nutritional Tracking UI")]
    public TextMeshProUGUI oilText;
    public TextMeshProUGUI sugarText;

    [Header("Game Win Panel")]
    public GameObject gameWinPanel;
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI starsText;

    [Header("Game Over Panel")]
    public GameObject timeGameOverPanel;
    public TextMeshProUGUI timeLoseScoreText;

    [Header("Starting & Pause Panels")]
    public GameObject difficultyPanel;
    public GameObject pausePanel;

    [Header("Popup Prefabs")]
    public GameObject healthyPopupPrefab;
    public GameObject unhealthyPopupPrefab;
    public GameObject comboPopupPrefab;

    [Header("Win Settings")]
    public int totalItemsToFinish = 8; // 2 items per 4 zones

    private int currentScore = 0;
    private int totalItemsPlaced = 0;
    private int healthyCombo = 0;

    // Floating point tracking based on your new SO
    private float currentOil = 0f;
    private float currentSugar = 0f;

    private float gameDuration;
    private float timeRemaining;
    private float timeElapsed;
    private bool isGameActive = false;

    [Header("Intro Settings")]
    public GameObject introScreen;
    public GameObject introVideo;
    public GameObject introVideoCanvas;

    private string currentDifficulty = "Normal";
    private static bool hasSeenIntro = true;

    private List<string> chosenFoodNames = new List<string>();
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (!hasSeenIntro)
        {
            if (introScreen != null) introScreen.SetActive(true);
            hasSeenIntro = true;
        }
        else
        {
            if (introScreen != null) introScreen.SetActive(false);
            if (introVideo != null) introVideo.SetActive(false);
            if (introVideoCanvas != null) introVideoCanvas.SetActive(false);
        }

        if (difficultyPanel != null) difficultyPanel.SetActive(true);
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (timeGameOverPanel != null) timeGameOverPanel.SetActive(false);

        isGameActive = false;
    }

    public void StartGameWithDifficulty(int minutes, string difficulty)
    {
        currentDifficulty = difficulty;
        gameDuration = minutes * 60f;
        timeRemaining = gameDuration;
        timeElapsed = 0f;

        currentScore = 0;
        totalItemsPlaced = 0;
        healthyCombo = 0;

        currentOil = 0f;
        currentSugar = 0f;

        chosenFoodNames.Clear();

        if (difficultyPanel != null) difficultyPanel.SetActive(false);

        UpdateTimerUI();
        UpdateScoreUI();
        UpdateNutritionUI();

        isGameActive = true;
    }

    void Update()
    {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        timeElapsed += Time.deltaTime;

        UpdateTimerUI();

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            GameOver(timeGameOverPanel, timeLoseScoreText, "Time");
        }
    }

    public void HandleCorrectPlacement(FoodItemSO food, Vector3 platePos)
    {
        if (!isGameActive) return;
        AudioManager.Instance.Play("DropSuccess");

        totalItemsPlaced++;

        chosenFoodNames.Add(food.foodName);

        // Add float values
        currentOil += food.oil;
        currentSugar += food.sugar;
        UpdateNutritionUI();

        if (food.isHealthy)
        {
            timeRemaining += 2f;
            ShowPopup(healthyPopupPrefab, "+2s", platePos);

            healthyCombo++;
            if (healthyCombo >= 3 && healthyCombo % 3 == 0)
            {
                AudioManager.Instance.Play("Combo");
                ShowPopup(comboPopupPrefab, "COMBO!", platePos + Vector3.up * 0.5f);
            }
        }
        else
        {
            healthyCombo = 0;
            timeRemaining -= 5f;
            if (timeRemaining < 0) timeRemaining = 0;
            ShowPopup(unhealthyPopupPrefab, "-5s", platePos);
        }

        UpdateTimerUI();

        // Trigger Final Score when all 8 zones are full
        if (totalItemsPlaced >= totalItemsToFinish)
        {
            CalculateFinalScoreAndWin();
        }
    }

    public void HandleWrongPlacement(Vector3 platePos)
    {
        if (!isGameActive) return;
        AudioManager.Instance.Play("DropFail");

        healthyCombo = 0;
        timeRemaining -= 5f;
        if (timeRemaining < 0) timeRemaining = 0;

        ShowPopup(unhealthyPopupPrefab, "-5s Penalty!", platePos);
        UpdateTimerUI();
    }

    private void ShowPopup(GameObject prefabToSpawn, string message, Vector3 position)
    {
        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = new Vector3(position.x, position.y, -1f);
            GameObject popup = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            TextMeshPro textMesh = popup.GetComponent<TextMeshPro>();
            if (textMesh != null) textMesh.text = message;
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
    }

    void UpdateNutritionUI()
    {
        // Display only the rounded whole number, without any extra text
        if (oilText != null) oilText.text = Mathf.RoundToInt(currentOil).ToString();
        if (sugarText != null) sugarText.text = Mathf.RoundToInt(currentSugar).ToString();
    }

    private void CalculateFinalScoreAndWin()
    {
        int finalScore = 60; // Base Score

        // Round the floats to ints so they perfectly match your table
        int totalOil = Mathf.RoundToInt(currentOil);
        int totalSugar = Mathf.RoundToInt(currentSugar);

        // --- Calculate Oil Score ---
        if (totalOil >= 5 && totalOil <= 8) finalScore += 20;
        else if ((totalOil >= 3 && totalOil <= 4) || (totalOil >= 9 && totalOil <= 10)) finalScore += 5;
        else finalScore -= 15; // 0-2 or 11-16

        // --- Calculate Sugar Score ---
        if (totalSugar >= 3 && totalSugar <= 6) finalScore += 20;
        else if (totalSugar == 2 || (totalSugar >= 7 && totalSugar <= 8)) finalScore += 5;
        else finalScore -= 15; // 0-1 or 9-16

        // Apply final score
        currentScore = finalScore;
        UpdateScoreUI();

        // --- Calculate Stars ---
        int stars = 0;
        if (currentScore >= 90) stars = 3;
        else if (currentScore >= 70) stars = 2;
        else if (currentScore >= 60) stars = 1;
        else stars = 0;

        if (starsText != null)
        {
            if (stars == 0) starsText.text = "Failed!";
            else starsText.text = stars.ToString() + " Stars!";
        }

        GameWin();
    }

    void GameOver(GameObject panelToActivate, TextMeshProUGUI scoreTextToUpdate, string reason)
    {
        isGameActive = false;
        AudioManager.Instance.Play("GameOver");
        if (panelToActivate != null) panelToActivate.SetActive(true);
        if (timerText != null && timeRemaining <= 0) timerText.text = "00:00";
        if (scoreTextToUpdate != null) scoreTextToUpdate.text = currentScore.ToString();

        // 🔥 Now it passes the actual reason ("Time") instead of a hardcoded string
        HandleExternalAPIs(false, reason);
    }

    void GameWin()
    {
        isGameActive = false;

        if (currentScore < 60) AudioManager.Instance.Play("GameOver");
        else AudioManager.Instance.Play("GameWin");

        if (gameWinPanel != null) gameWinPanel.SetActive(true);
        UpdateTimerUI();
        if (winScoreText != null) winScoreText.text = currentScore.ToString();

        HandleExternalAPIs(true, "None");
    }

    private void HandleExternalAPIs(bool completed, string lossReason)
    {
        // GM3 / food-choose cloud sync removed in Phase 2
    }

    // Video/UI Callbacks
    public void OffIntroScreen() { introScreen.SetActive(false); introVideo.SetActive(false); }
    public void OnIntroVideo() { introVideo.SetActive(true); introVideoCanvas.SetActive(true); }
    public void StartEasyGame() { StartGameWithDifficulty(3, "Easy"); }
    public void StartNormalGame() { StartGameWithDifficulty(2, "Normal"); }
    public void StartHardGame() { StartGameWithDifficulty(1, "Hard"); }

    public void PlayAgain()
    {
        ResetTrackers();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void PauseGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isGameActive = true;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void GoToMainMenu()
    {
        ResetTrackers();
        hasSeenIntro = false;
        SceneManager.LoadScene("MainMenu");
    }

    private void ResetTrackers()
    {
        PoseVisuallizer3D poseVis = Object.FindFirstObjectByType<PoseVisuallizer3D>();
        if (poseVis != null) poseVis.enabled = false;

        WebCamInput webCamInput = Object.FindFirstObjectByType<WebCamInput>();
        if (webCamInput != null) webCamInput.StopWebCam();

        Time.timeScale = 1f;
    }
}