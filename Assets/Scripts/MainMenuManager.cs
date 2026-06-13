using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;

    [Header("Music")]
    public AudioClip backgroundMusicClip;

    [Header("Button Sounds")]
    public AudioClip buttonClickSound;

    [Header("Authentication Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;
    public GameObject gameChoosePanel;
    public GameObject loadingPanel;
    public GameObject errorPanel;

    [Header("Login Panel Elements")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginSubmitButton;
    public Button loginToSignupButton;

    [Header("Signup Panel Elements")]
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupPasswordInput;
    public Button signupSubmitButton;
    public Button signupToLoginButton;

    [Header("Game Choose Panel Elements")]
    public Button mahjongGameButton;
    public Button taichiGameButton;
    public Button foodChoose;
    public Button memoryMartGameButton;

    [Header("Game Start Panel Elements")]
    public GameObject gameStartPanel;
    public Button backFromGameStartButton;

    [Header("Loading Panel Elements")]
    public TextMeshProUGUI loadingText;

    [Header("Error Panel Elements")]
    public TextMeshProUGUI errorText;
    public Button errorRetryButton;

    [Header("MJInfo Panel")]
    public Button infoButton;
    public GameObject instructionPanel;
    public Button gotItButton;

    [Header("PTInfo Panel")]
    public Button PTinfoButton;
    public GameObject PTinstructionPanel;
    public Button PTgotItButton;

    [Header("Sound Toggle")]
    public Button soundToggleButton;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    [Header("Language")]
    public Button languageButton;

    [Header("Logout")]
    public Button logoutButton;

    [Header("Session Info (Debug)")]
    public TextMeshProUGUI sessionInfoText;

    // Private variables
    private AudioSource buttonAudioSource;
    private GameObject previousPanel;
    private bool isProcessing = false;

    void Start()
    {
        SetupAuthentication();
        SetupInfoButton();
        SetupButtonAudio();
        SetupSoundToggle();
        InitializeBackgroundMusic();

        RefreshLanguageManager();
        CheckSessionAndShowPanel();
    }

    void OnEnable()
    {
        BackendFacade.OnAuthenticationResult += OnAuthenticationComplete;
        BackendFacade.OnAuthenticationError += OnAuthenticationError;
        BackendFacade.OnRegistrationResult += OnRegistrationComplete;
        BackendFacade.OnRegistrationError += OnRegistrationError;
        BackendFacade.OnSessionExpired += OnSessionExpired;
        BackendFacade.OnUploadFailed += OnUploadFailed;
    }

    void OnDisable()
    {
        BackendFacade.OnAuthenticationResult -= OnAuthenticationComplete;
        BackendFacade.OnAuthenticationError -= OnAuthenticationError;
        BackendFacade.OnRegistrationResult -= OnRegistrationComplete;
        BackendFacade.OnRegistrationError -= OnRegistrationError;
        BackendFacade.OnSessionExpired -= OnSessionExpired;
        BackendFacade.OnUploadFailed -= OnUploadFailed;
    }

    void CheckSessionAndShowPanel()
    {
        if (BackendFacade.Instance != null && BackendFacade.Instance.IsUserLoggedIn())
        {
            Debug.Log("User has valid session, showing game choose panel");
            ShowGameChoosePanel();
            UpdateSessionInfo();
        }
        else
        {
            Debug.Log("No valid session, showing login panel");
            ShowLoginPanel();
        }
    }

    void UpdateSessionInfo()
    {
        if (sessionInfoText != null && BackendFacade.Instance != null)
        {
            sessionInfoText.text = BackendFacade.Instance.GetSessionInfo();
        }
    }

    void OnSessionExpired()
    {
        Debug.Log("Session expired, returning to login");
        ShowLoginPanel();
        ShowErrorPanel("Your session has expired. Please login again.");
    }

    void OnUploadFailed(string message)
    {
        ShowErrorPanel(message);
    }

    #region Authentication Setup

    void SetupAuthentication()
    {
        HideAllPanels();
    }

    #endregion

    #region Panel Management

    void HideAllPanels()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signupPanel != null) signupPanel.SetActive(false);
        if (gameChoosePanel != null) gameChoosePanel.SetActive(false);
        if (gameStartPanel != null) gameStartPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    public void ShowLoginPanel()
    {
        PlayButtonSound();
        HideAllPanels();

        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
            previousPanel = loginPanel;

            if (loginEmailInput != null) loginEmailInput.text = "";
            if (loginPasswordInput != null) loginPasswordInput.text = "";
        }
    }

    public void ShowSignupPanel()
    {
        PlayButtonSound();
        HideAllPanels();

        if (signupPanel != null)
        {
            signupPanel.SetActive(true);
            previousPanel = signupPanel;

            if (signupEmailInput != null) signupEmailInput.text = "";
            if (signupPasswordInput != null) signupPasswordInput.text = "";
        }
    }

    public void ShowGameChoosePanel()
    {
        PlayButtonSound();
        HideAllPanels();

        if (gameChoosePanel != null)
        {
            gameChoosePanel.SetActive(true);
            previousPanel = gameChoosePanel;
            UpdateSessionInfo();
        }
    }

    public void ShowGameStartPanel()
    {
        PlayButtonSound();
        HideAllPanels();

        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(true);
            previousPanel = gameStartPanel;
            Debug.Log("Game Start Panel shown - Player can now configure and start Mahjong game");
        }
        else
        {
            Debug.LogWarning("Game Start Panel not assigned. Falling back to direct scene load.");
            StartMahjongGameDirect();
        }
    }

    public void ShowLoadingPanel(string message = "Please wait...")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
                loadingText.text = message;
        }
    }

    public void HideLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void ShowErrorPanel(string errorMessage)
    {
        HideLoadingPanel();

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            if (errorText != null)
                errorText.text = errorMessage;
        }
    }

    #endregion

    #region Authentication Methods

    public async void OnLoginSubmit()
    {
        if (isProcessing) return;

        PlayButtonSound();

        string email = loginEmailInput?.text?.Trim();
        string password = loginPasswordInput?.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowErrorPanel("Please enter both email and password");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorPanel("Please enter a valid email address");
            return;
        }

        if (BackendFacade.Instance == null)
        {
            ShowErrorPanel("Backend not initialized. Please restart the game.");
            return;
        }

        if (!BackendFacade.Instance.IsConfigured)
        {
            ShowErrorPanel(
                "API not configured. Copy Resources/WatchSdkConfig.example.json to " +
                "WatchSdkConfig.local.json and set apiBaseUrl.");
            return;
        }

        isProcessing = true;
        ShowLoadingPanel("Logging in...");

        await BackendFacade.Instance.LoginUser(email, password);
        isProcessing = false;
    }

    public async void OnSignupSubmit()
    {
        if (isProcessing) return;

        PlayButtonSound();

        string email = signupEmailInput?.text?.Trim();
        string password = signupPasswordInput?.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowErrorPanel("Please enter both email and password");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorPanel("Please enter a valid email address");
            return;
        }

        if (password.Length < 6)
        {
            ShowErrorPanel("Password must be at least 6 characters");
            return;
        }

        if (BackendFacade.Instance == null)
        {
            ShowErrorPanel("Backend not initialized. Please restart the game.");
            return;
        }

        if (!BackendFacade.Instance.IsConfigured)
        {
            ShowErrorPanel(
                "API not configured. Copy Resources/WatchSdkConfig.example.json to " +
                "WatchSdkConfig.local.json and set apiBaseUrl.");
            return;
        }

        isProcessing = true;
        ShowLoadingPanel("Creating account...");

        await BackendFacade.Instance.RegisterUser(email, password);
        isProcessing = false;
    }

    #endregion

    #region Backend Event Handlers

    void OnAuthenticationComplete(bool success)
    {
        isProcessing = false;
        HideLoadingPanel();

        if (success)
        {
            Debug.Log("Authentication successful");
            ShowGameChoosePanel();
        }
    }

    void OnAuthenticationError(string error)
    {
        isProcessing = false;
        ShowErrorPanel(error);
    }

    void OnRegistrationComplete(bool success)
    {
        isProcessing = false;
        HideLoadingPanel();

        if (success)
        {
            Debug.Log("Registration successful — signed in");
            ShowGameChoosePanel();
        }
    }

    void OnRegistrationError(string error)
    {
        isProcessing = false;
        ShowErrorPanel(error);
    }

    #endregion

    #region Game Navigation

    public void StartMahjongGameDirect()
    {
        PlayButtonSound();

        PoseVisuallizer3D poseVis = Object.FindFirstObjectByType<PoseVisuallizer3D>();
        if (poseVis != null)
        {
            poseVis.enabled = false;
        }

        WebCamInput webCamInput = Object.FindFirstObjectByType<WebCamInput>();
        if (webCamInput != null)
        {
            webCamInput.StopWebCam();
        }
        try
        {
            SceneManager.LoadScene(1);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load Mahjong game scene: {e.Message}");
            ShowErrorPanel("Failed to load game. Please try again.");
        }
    }

    public void StartTaichiGame()
    {
        PlayButtonSound();

        PoseVisuallizer3D poseVis = Object.FindFirstObjectByType<PoseVisuallizer3D>();
        if (poseVis != null)
        {
            poseVis.enabled = false;
        }

        WebCamInput webCamInput = Object.FindFirstObjectByType<WebCamInput>();
        if (webCamInput != null)
        {
            webCamInput.StopWebCam();
        }
        try
        {
            SceneManager.LoadScene("TaichiGame");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load TaichiGame scene: {e.Message}");
            ShowErrorPanel("TaichiGame scene not found. Please add it to Build Settings.");
        }
    }

    public void StartFoodChooseGame()
    {
        PlayButtonSound();

        PoseVisuallizer3D poseVis = Object.FindFirstObjectByType<PoseVisuallizer3D>();
        if (poseVis != null)
        {
            poseVis.enabled = false;
        }

        WebCamInput webCamInput = Object.FindFirstObjectByType<WebCamInput>();
        if (webCamInput != null)
        {
            webCamInput.StopWebCam();
        }
        try
        {
            SceneManager.LoadScene("HealthyDiet");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load FoodChoose scene: {e.Message}");
            ShowErrorPanel("FoodChoose scene not found. Please add it to Build Settings.");
        }
    }

    public void StartMemoryMartGame()
    {
        PlayButtonSound();

        PoseVisuallizer3D poseVis = Object.FindFirstObjectByType<PoseVisuallizer3D>();
        if (poseVis != null)
        {
            poseVis.enabled = false;
        }

        WebCamInput webCamInput = Object.FindFirstObjectByType<WebCamInput>();
        if (webCamInput != null)
        {
            webCamInput.StopWebCam();
        }
        try
        {
            SceneManager.LoadScene("MemoryMart");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load MemoryMart scene: {e.Message}");
            ShowErrorPanel("MemoryMart scene not found. Please add it to Build Settings.");
        }
    }

    public void OnGameStartFromPanel()
    {
        PlayButtonSound();
        Debug.Log("Starting Mahjong game from Game Start Panel");

        PoseVisuallizer3D poseVis = Object.FindFirstObjectByType<PoseVisuallizer3D>();
        if (poseVis != null)
        {
            poseVis.enabled = false;
        }

        WebCamInput webCamInput = Object.FindFirstObjectByType<WebCamInput>();
        if (webCamInput != null)
        {
            webCamInput.StopWebCam();
        }
        try
        {
            SceneManager.LoadScene(1);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game scene: {e.Message}");
            ShowErrorPanel("Failed to start game. Please try again.");
        }
    }

    #endregion

    #region Error Handling

    public void OnErrorRetry()
    {
        PlayButtonSound();

        if (errorPanel != null)
            errorPanel.SetActive(false);

        if (previousPanel != null)
            previousPanel.SetActive(true);
    }

    #endregion

    #region Utility Methods

    bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void RefreshLanguageManager()
    {
        LanguageManager languageManager = LanguageManager.EnsureInstance();

        if (languageManager != null)
        {
            Debug.Log("MainMenuManager: LanguageManager ensured to exist, refreshing language");

            if (languageManager.IsInitialized())
            {
                languageManager.RefreshCurrentLanguage();
            }
            else
            {
                Debug.Log("MainMenuManager: LanguageManager not fully initialized, forcing reinitialize");
                languageManager.ForceReinitialize();
            }
        }
        else
        {
            Debug.LogError("MainMenuManager: Failed to ensure LanguageManager instance!");
        }
    }

    #endregion

    #region Existing Methods (Keep as they were)

    void SetupInfoButton()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }

    public void ShowInstructions()
    {
        PlayButtonSound();

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
    }

    public void HideInstructions()
    {
        PlayButtonSound();

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }

    void SetupPTInfoButton()
    {
        if (PTinstructionPanel != null)
        {
            PTinstructionPanel.SetActive(false);
        }
    }

    public void ShowPTInstructions()
    {
        PlayButtonSound();

        if (PTinstructionPanel != null)
        {
            PTinstructionPanel.SetActive(true);
        }
    }

    public void HidePTInstructions()
    {
        PlayButtonSound();

        if (PTinstructionPanel != null)
        {
            PTinstructionPanel.SetActive(false);
        }
    }

    void SetupButtonAudio()
    {
        if (buttonClickSound != null)
        {
            buttonAudioSource = gameObject.AddComponent<AudioSource>();
            buttonAudioSource.clip = buttonClickSound;
            buttonAudioSource.volume = 0.7f;
            buttonAudioSource.playOnAwake = false;
        }
    }

    private void SetupSoundToggle()
    {
        if (soundToggleButton != null)
        {
            GlobalSoundToggle soundToggle = soundToggleButton.GetComponent<GlobalSoundToggle>();
            if (soundToggle == null)
            {
                soundToggle = soundToggleButton.gameObject.AddComponent<GlobalSoundToggle>();
            }

            soundToggle.soundButton = soundToggleButton;
            soundToggle.soundOnSprite = soundOnSprite;
            soundToggle.soundOffSprite = soundOffSprite;

            Debug.Log("Sound toggle setup complete");
        }
    }

    void InitializeBackgroundMusic()
    {
        if (MusicManager.Instance == null && backgroundMusicClip != null)
        {
            GameObject musicManagerObj = new GameObject("MusicManager");
            MusicManager musicManager = musicManagerObj.AddComponent<MusicManager>();
            musicManager.backgroundMusic = backgroundMusicClip;
        }
    }

    public void OnLogoutButtonClick()
    {
        PlayButtonSound();

        if (BackendFacade.Instance != null)
        {
            BackendFacade.Instance.LogoutUser();
            ShowLoginPanel();

            if (sessionInfoText != null)
                sessionInfoText.text = "";
        }
        else
        {
            Debug.LogError("BackendFacade not found");
        }
    }

    public void QuitGame()
    {
        PlayButtonSound();

        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void PlayButtonSound()
    {
        if (buttonAudioSource != null && buttonClickSound != null)
        {
            buttonAudioSource.PlayOneShot(buttonClickSound);
        }
    }

    #endregion

    #region Public Methods for Inspector OnClick Events

    public void OnLoginSubmitClick() => OnLoginSubmit();
    public void OnSignupSubmitClick() => OnSignupSubmit();

    public void ShowLoginPanelClick() => ShowLoginPanel();
    public void ShowSignupPanelClick() => ShowSignupPanel();
    public void ShowGameChoosePanelClick() => ShowGameChoosePanel();
    public void ShowGameStartPanelClick() => ShowGameStartPanel();

    public void SelectMahjongGameClick()
    {
        PlayButtonSound();
        ShowGameStartPanel();
    }

    public void SelectTaichiGameClick()
    {
        PlayButtonSound();
        StartTaichiGame();
    }

    public void SelectFoodChooseGameClick()
    {
        PlayButtonSound();
        StartFoodChooseGame();
    }

    public void ShowInstructionsClick() => ShowInstructions();
    public void HideInstructionsClick() => HideInstructions();

    public void OnErrorRetryClick() => OnErrorRetry();

    public void OnLogoutButtonClickEvent() => OnLogoutButtonClick();

    public void OnGameStartFromPanelClick() => OnGameStartFromPanel();

    public void QuitGameClick() => QuitGame();

    #endregion
}
