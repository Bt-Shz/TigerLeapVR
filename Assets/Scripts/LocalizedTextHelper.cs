using UnityEngine;
using UnityEngine.Localization.Components;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Helper script to easily setup localized text components
/// This script automatically configures Localize String Event components
/// </summary>
[System.Serializable]
public class LocalizedTextHelper : MonoBehaviour
{
    [Header("Localization Settings")]
    [SerializeField] private string tableCollectionName = "MainMenuTexts";
    [SerializeField] private string entryKey;
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool showDebugLogs = false;
    
    private LocalizeStringEvent localizeStringEvent;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupLocalizedText();
        }
    }
    
    /// <summary>
    /// Automatically sets up the localized text component
    /// </summary>
    [ContextMenu("Setup Localized Text")]
    public void SetupLocalizedText()
    {
        if (string.IsNullOrEmpty(entryKey))
        {
            Debug.LogWarning($"LocalizedTextHelper: Entry key is empty on {gameObject.name}");
            return;
        }
        
        // Get or add LocalizeStringEvent component
        localizeStringEvent = GetComponent<LocalizeStringEvent>();
        if (localizeStringEvent == null)
        {
            localizeStringEvent = gameObject.AddComponent<LocalizeStringEvent>();
        }
        
        // Setup the string reference
        localizeStringEvent.StringReference.SetReference(tableCollectionName, entryKey);
        
        if (showDebugLogs)
        {
            Debug.Log($"LocalizedTextHelper: Setup complete for {gameObject.name} with key '{entryKey}'");
        }
    }
    
    /// <summary>
    /// Changes the localization key at runtime
    /// </summary>
    /// <param name="newKey">New localization key</param>
    public void ChangeLocalizationKey(string newKey)
    {
        entryKey = newKey;
        SetupLocalizedText();
    }
    
    /// <summary>
    /// Gets the current localization key
    /// </summary>
    /// <returns>Current localization key</returns>
    public string GetLocalizationKey()
    {
        return entryKey;
    }
    
    #region Editor Helper Methods
    
    /// <summary>
    /// Common localization keys for quick setup
    /// </summary>
    [System.Serializable]
    public static class CommonKeys
    {
        public const string TITLE_GAME = "title_game";
        public const string BUTTON_LOGIN = "button_login";
        public const string BUTTON_SIGNUP = "button_signup";
        public const string BUTTON_PLAY = "button_play";
        public const string BUTTON_SETTINGS = "button_settings";
        public const string BUTTON_QUIT = "button_quit";
        public const string BUTTON_BACK = "button_back";
        public const string BUTTON_LANGUAGE = "button_language";
        public const string LABEL_EMAIL = "label_email";
        public const string LABEL_PASSWORD = "label_password";
        public const string TEXT_FORGOT_PASSWORD = "text_forgot_password";
        public const string TEXT_LOADING = "text_loading";
        public const string TEXT_WELCOME = "text_welcome";
        public const string LANGUAGE_ENGLISH = "language_english";
        public const string LANGUAGE_CHINESE = "language_chinese";
        public const string LANGUAGE_HINDI = "language_hindi";
        public const string MAHJONG_GAME = "mahjong_game";
        public const string TAICHI_GAME = "taichi_game";
        public const string INSTRUCTIONS_TITLE = "instructions_title";
        public const string GOT_IT = "got_it";
    }
    
    /// <summary>
    /// Quick setup methods for common UI elements
    /// </summary>
    [ContextMenu("Quick Setup/Title")]
    private void SetupAsTitle() { entryKey = CommonKeys.TITLE_GAME; SetupLocalizedText(); }
    
    [ContextMenu("Quick Setup/Login Button")]
    private void SetupAsLoginButton() { entryKey = CommonKeys.BUTTON_LOGIN; SetupLocalizedText(); }
    
    [ContextMenu("Quick Setup/Back Button")]
    private void SetupAsBackButton() { entryKey = CommonKeys.BUTTON_BACK; SetupLocalizedText(); }
    
    [ContextMenu("Quick Setup/Language Button")]
    private void SetupAsLanguageButton() { entryKey = CommonKeys.BUTTON_LANGUAGE; SetupLocalizedText(); }
    
    #endregion
}
