using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PTMenuManager : MonoBehaviour
{
    [Header("In-Game Buttons")]
    [Tooltip("Button to exit to main menu during game")]
    public Button inGameExitButton;

    [Tooltip("Button to pause the game")]
    public Button pauseButton;

    [Header("Icons")]
    [Tooltip("Icon to display on pause button when game is running")]
    public Sprite pauseIcon;
    [Tooltip("Icon to display on pause button when game is paused")]
    public Sprite playIcon;
    
    [Header("Game Components")]
    [Tooltip("Reference to PTSpawnManager")]
    public PTSpawnManager spawnManager;
    
    [Tooltip("Reference to PTScoreManager")]
    public PTScoreManager scoreManager;
    
    [Header("Audio")]
    [Tooltip("Button click sound")]
    public AudioClip buttonClickSound;
    
    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebug = false;
    
    [Header("Gate System")]
    [Tooltip("Reference to PTGateSpawnManager")]
    public PTGateSpawnManager gateSpawnManager;
    
    private AudioSource audioSource;
    private bool gameStarted = false;
    
    void Start()
    {
        SetupMenu();
        SetupAudio();
        StartGame();
    }
    
    /// <summary>
    /// Sets up the menu system
    /// </summary>
    private void SetupMenu()
    {
        // Setup button listeners
        if (inGameExitButton != null)
        {
            inGameExitButton.onClick.AddListener(OnQuitButtonClicked);
            inGameExitButton.gameObject.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
            pauseButton.gameObject.SetActive(false);
        }
        
        // Ensure game components are stopped initially
        if (spawnManager != null)
        {
            spawnManager.StopSpawning();
        }
        
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Menu setup complete");
        }
    }
    
    /// <summary>
    /// Sets up audio source for button sounds
    /// </summary>
    private void SetupAudio()
    {
        if (buttonClickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = buttonClickSound;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
        }
    }
    
    /// <summary>
    /// Shows the menu panel (Deprecated - only used for pause logic now)
    /// </summary>
    public void ShowMenu()
    {
        // Pause the game time
        Time.timeScale = 0f;
        
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Game Paused");
        }
    }
    
    /// <summary>
    /// Hides the menu panel (Deprecated - only used for resume logic now)
    /// </summary>
    public void HideMenu()
    {
        // Resume game time
        Time.timeScale = 1f;
        
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Game Resumed");
        }
    }
    
    /// <summary>
    /// Called when Quit button is clicked
    /// </summary>
    public void OnQuitButtonClicked()
    {
        PlayButtonSound();
        
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Quit button clicked - Loading MainMenu");
        }
        
        LoadMainMenu();
    }

    /// <summary>
    /// Called when Pause button is clicked
    /// </summary>
    public void OnPauseButtonClicked()
    {
        PlayButtonSound();
        
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Pause button clicked");
        }
        
        TogglePause();
    }

    /// <summary>
    /// Updates the pause button icon based on state
    /// </summary>
    /// <param name="isPaused">True if game is paused (show play icon), false if running (show pause icon)</param>
    private void UpdatePauseButtonIcon(bool isPaused)
    {
        if (pauseButton != null)
        {
            Image btnImage = pauseButton.GetComponent<Image>();
            if (btnImage != null)
            {
                if (isPaused && playIcon != null)
                {
                    btnImage.sprite = playIcon;
                }
                else if (!isPaused && pauseIcon != null)
                {
                    btnImage.sprite = pauseIcon;
                }
            }
        }
    }
    
    /// <summary>
    /// Starts the game
    /// </summary>
    private void StartGame()
    {
        gameStarted = true;
    // Ensure GM2 is selected for Firebase writes
    if (FirebaseManager.Instance != null) FirebaseManager.Instance.SelectTaichiGame();
    // Reset game manager state
    if (PTGameManager.Instance != null) PTGameManager.Instance.ResetRun();
        
        // Reset score for new game
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        else if (PTScoreManager.Instance != null)
        {
            PTScoreManager.Instance.ResetScore();
        }

        // Show HUD buttons
        if (inGameExitButton != null) inGameExitButton.gameObject.SetActive(true);
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
            UpdatePauseButtonIcon(false);
        }
        
        // Hide the menu
    HideMenu();
    Time.timeScale = 1f;
        
        // Start spawning cubes
        if (spawnManager != null)
        {
            spawnManager.StartSpawning();
        }
        
        /* 
        // Start spawning gates - Disabled as per request
        if (gateSpawnManager != null)
        {
            gateSpawnManager.StartSpawning();
        }
        */
        
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Game started successfully");
        }
    }
    
    /// <summary>
    /// Loads the MainMenu scene
    /// </summary>
    private void LoadMainMenu()
    {
        if (showDebug)
        {
            Debug.Log("PTMenuManager: Loading MainMenu scene...");
        }
        
        // Stop any ongoing game processes
        if (spawnManager != null)
        {
            spawnManager.StopSpawning();
        }
        
        if (gateSpawnManager != null)
        {
            gateSpawnManager.StopSpawning();
        }
        
        // Destroy remaining objects
        DestroyRemainingCubes();
        DestroyRemainingGates();

        // Hide HUD buttons
        if (inGameExitButton != null) inGameExitButton.gameObject.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load MainMenu scene
        try
        {
            SceneManager.LoadScene("MainMenu");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PTMenuManager: Failed to load MainMenu scene: {e.Message}");
            // Fallback: try loading by index
            SceneManager.LoadScene(0);
        }
    }
    
    /// <summary>
    /// Plays button click sound
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// Public method to show menu (can be called from other scripts)
    /// </summary>
    public void ShowMainMenu()
    {
        gameStarted = false;
        
        // Stop spawning
        if (spawnManager != null)
        {
            spawnManager.StopSpawning();
        }
        
        // Stop gate spawning
        if (gateSpawnManager != null)
        {
            gateSpawnManager.StopSpawning();
        }
        
        // Destroy any remaining cubes and gates
        DestroyRemainingCubes();
        DestroyRemainingGates();
        
        // Redirect to MainMenu scene since in-game menu is removed
        LoadMainMenu();
    }
    
    /// <summary>
    /// Destroys any remaining moving cubes
    /// </summary>
    private void DestroyRemainingCubes()
    {
        GameObject[] movingCubes = GameObject.FindGameObjectsWithTag("MovingCube");
        foreach (GameObject cube in movingCubes)
        {
            Destroy(cube);
        }
        
        if (showDebug && movingCubes.Length > 0)
        {
            Debug.Log($"PTMenuManager: Destroyed {movingCubes.Length} remaining cubes");
        }
    }
    
    /// <summary>
    /// Destroys any remaining decorative gates
    /// </summary>
    private void DestroyRemainingGates()
    {
        if (gateSpawnManager != null)
        {
            gateSpawnManager.DestroyAllGates();
        }
        
        // Also clean up any gates that might not be tracked
        GameObject[] decorativeGates = GameObject.FindGameObjectsWithTag("DecorativeGate");
        foreach (GameObject gate in decorativeGates)
        {
            Destroy(gate);
        }
        
        if (showDebug && decorativeGates.Length > 0)
        {
            Debug.Log($"PTMenuManager: Destroyed {decorativeGates.Length} remaining gates");
        }
    }
    
    /// <summary>
    /// Check if game is currently started
    /// </summary>
    public bool IsGameStarted()
    {
        return gameStarted;
    }
    
    /// <summary>
    /// Handle pause functionality (optional)
    /// </summary>
    public void TogglePause()
    {
        if (gameStarted)
        {
            if (Time.timeScale == 0f)
            {
                // Resume
                HideMenu();
                UpdatePauseButtonIcon(false);
            }
            else
            {
                // Pause
                Time.timeScale = 0f;
                UpdatePauseButtonIcon(true);
                if (showDebug)
                {
                    Debug.Log("PTMenuManager: Game Paused (No Menu)");
                }
            }
        }
    }
    
    void Update()
    {
        // Optional: Handle ESC key to show/hide menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameStarted)
            {
                TogglePause();
            }
        }
    }
}