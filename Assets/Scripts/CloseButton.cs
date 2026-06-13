using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CloseButton : MonoBehaviour
{
    [Header("Close Button Settings")]
    public GameObject targetPanel;           // Panel to close
    public bool isTargetPanelNotNeeded = false; // Added parameter (False by default = panel is needed)
    public CloseButtonType buttonType;       // Type of close action
    public bool playSound = true;            // Play button click sound

    [Header("Delay Settings")]
    public float closeDelay = 0.6f;         // Delay before closing (in seconds)

    [Header("Optional Custom Actions")]
    public UnityEngine.Events.UnityEvent customCloseAction; // Custom actions on close

    private Button button;

    public enum CloseButtonType
    {
        SimpleClose,        // Just close the panel
        ResumeGame,         // Close and resume game (for pause panel)
        RestartGame,        // Close and restart game
        LoadMainMenu,       // Close and go to main menu
        CustomAction,       // Use custom action

    }

    void Start()
    {
        // Get button component
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnCloseButtonClicked);
        }
        else
        {
            Debug.LogError("CloseButton script requires a Button component!");
        }
    }

    public void OnCloseButtonClicked()
    {
        Debug.Log("Close button clicked!");

        // Play button sound immediately
        if (playSound)
        {
            PlayButtonSound();
        }

        // Start the delayed close coroutine
        StartCoroutine(DelayedClose());
    }

    private IEnumerator DelayedClose()
    {
        Debug.Log($"Starting delay of {closeDelay} seconds...");

        // Wait for the specified delay (using unscaled time since game is paused)
        yield return new WaitForSecondsRealtime(closeDelay);

        Debug.Log($"Delay finished. Button type: {buttonType}");

        // Perform action based on button type
        switch (buttonType)
        {
            case CloseButtonType.SimpleClose:
                Debug.Log("Executing SimpleClose");
                SimpleClosePanel();
                break;

            case CloseButtonType.ResumeGame:
                Debug.Log("Executing ResumeGame");
                ResumeGameAndClose();
                break;

            case CloseButtonType.RestartGame:
                Debug.Log("Executing RestartGame");
                RestartGame();
                break;

            case CloseButtonType.LoadMainMenu:
                Debug.Log("Executing LoadMainMenu");
                LoadMainMenu();
                break;

            case CloseButtonType.CustomAction:
                Debug.Log("Executing CustomAction");
                ExecuteCustomAction();
                break;
        }
    }

    private void SimpleClosePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
            Debug.Log($"Closed panel: {targetPanel.name}");
        }
    }

    private void ResumeGameAndClose()
    {
        Debug.Log("ResumeGameAndClose called");

        if (GameManager.Instance != null)
        {
            Debug.Log("Calling GameManager.ResumeGame()");
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }

        if (targetPanel != null)
        {
            Debug.Log($"Closing panel: {targetPanel.name}");
            targetPanel.SetActive(false);
        }
        else if (!isTargetPanelNotNeeded) // Updated here: Only show error if the panel is actually needed
        {
            Debug.LogError("Target panel is null!");
        }
    }

    private void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void LoadMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
    }

    private void ExecuteCustomAction()
    {
        customCloseAction?.Invoke();

        // Still close the panel after custom action
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }



    private void PlayButtonSound()
    {
        // Check PersistentSoundManager first
        bool soundEnabled = true;
        if (PersistentSoundManager.Instance != null)
        {
            soundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
        }

        if (soundEnabled)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }

    void OnDestroy()
    {
        // Clean up listener
        if (button != null)
        {
            button.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }
}