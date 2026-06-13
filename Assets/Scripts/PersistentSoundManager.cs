using UnityEngine;
using UnityEngine.SceneManagement;
#if MM_TOOLS_PRESENT
using MoreMountains.Tools;
#endif

public class PersistentSoundManager : MonoBehaviour
{
    public static PersistentSoundManager Instance;

    [Header("Sound State")]
    public bool isGlobalSoundEnabled = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved sound preference
            LoadSoundSettings();

            // Apply sound settings immediately
            ApplyGlobalSoundState();

            if (showDebugLogs)
                Debug.Log($"🌍 PersistentSoundManager created - Sound enabled: {isGlobalSoundEnabled}");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("🌍 PersistentSoundManager duplicate destroyed");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Subscribe to scene loaded event (replaces deprecated OnLevelWasLoaded)
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Apply sound settings again after all other components are initialized
        Invoke(nameof(ApplyGlobalSoundState), 0.1f);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called when a new scene is loaded
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reapply sound settings when scene changes
        Invoke(nameof(ApplyGlobalSoundState), 0.1f);

        if (showDebugLogs)
            Debug.Log($"🌍 Scene {scene.name} loaded - Reapplying sound settings");
    }

    // Toggle sound globally
    public void ToggleGlobalSound()
    {
        isGlobalSoundEnabled = !isGlobalSoundEnabled;

        if (showDebugLogs)
            Debug.Log($"🔊 PersistentSoundManager: Sound toggled to {isGlobalSoundEnabled}");

        // Save the preference
        SaveSoundSettings();

        // Apply immediately
        ApplyGlobalSoundState();

        // Notify all sound toggle buttons to update their visuals
        NotifyAllSoundToggleButtons();
    }

    // Set sound state directly
    public void SetGlobalSoundEnabled(bool enabled)
    {
        if (isGlobalSoundEnabled != enabled)
        {
            isGlobalSoundEnabled = enabled;
            SaveSoundSettings();
            ApplyGlobalSoundState();
            NotifyAllSoundToggleButtons();
        }
    }

    // Apply sound settings to ALL audio systems
    private void ApplyGlobalSoundState()
    {
        float targetVolume = isGlobalSoundEnabled ? 1f : 0f;

        if (showDebugLogs)
            Debug.Log($"🎵 Applying global sound state - AudioListener.volume: {targetVolume}");

        // Method 1: Control Unity's AudioListener (affects ALL Unity audio)
        AudioListener.volume = targetVolume;

        // Method 2: Control AudioManager if it exists (wrapped in try-catch for safety)
        ControlAudioManager();

        // Method 3: Control MMSoundManager (Feel system) - only if MoreMountains.Tools is present
#if MM_TOOLS_PRESENT
        ControlMMSoundManager();
#endif

        // Method 4: Control all AudioSources directly
        ControlAllAudioSources();
    }

    // Control your custom AudioManager (if it exists)
    private void ControlAudioManager()
    {
        // Check if AudioManager type exists using reflection to avoid compilation errors
        System.Type audioManagerType = System.Type.GetType("AudioManager");
        if (audioManagerType != null)
        {
            var audioManagerInstance = audioManagerType.GetProperty("Instance")?.GetValue(null);
            if (audioManagerInstance != null)
            {
                var soundEnabledProp = audioManagerType.GetProperty("isGlobalSoundEnabled");
                if (soundEnabledProp != null)
                {
                    soundEnabledProp.SetValue(audioManagerInstance, isGlobalSoundEnabled);

                    if (showDebugLogs)
                        Debug.Log($"🎵 AudioManager updated - Sound enabled: {isGlobalSoundEnabled}");
                }
            }
        }
    }

#if MM_TOOLS_PRESENT
    // Control MMSoundManager from Feel system
    private void ControlMMSoundManager()
    {
        var mmSoundManager = FindObjectOfType<MMSoundManager>();
        if (mmSoundManager != null)
        {
            if (isGlobalSoundEnabled)
            {
                // Unmute all tracks
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.UnmuteTrack, MMSoundManager.MMSoundManagerTracks.Master);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.UnmuteTrack, MMSoundManager.MMSoundManagerTracks.Music);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.UnmuteTrack, MMSoundManager.MMSoundManagerTracks.Sfx);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.UnmuteTrack, MMSoundManager.MMSoundManagerTracks.UI);
            }
            else
            {
                // Mute all tracks
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.MuteTrack, MMSoundManager.MMSoundManagerTracks.Master);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.MuteTrack, MMSoundManager.MMSoundManagerTracks.Music);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.MuteTrack, MMSoundManager.MMSoundManagerTracks.Sfx);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.MuteTrack, MMSoundManager.MMSoundManagerTracks.UI);
            }
           
            if (showDebugLogs)
                Debug.Log($"🎵 MMSoundManager controlled - Sound enabled: {isGlobalSoundEnabled}");
        }
    }
#endif

    // Control all AudioSources in the scene
    private void ControlAllAudioSources()
    {
        // Use version-compatible method
        AudioSource[] allAudioSources;

#if UNITY_2023_1_OR_NEWER
        allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
#else
        allAudioSources = FindObjectsOfType<AudioSource>();
#endif

        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.mute = !isGlobalSoundEnabled;
            }
        }

        if (showDebugLogs)
            Debug.Log($"🔊 Controlled {allAudioSources.Length} AudioSources - Muted: {!isGlobalSoundEnabled}");
    }

    // Notify all sound toggle buttons to update their visuals
    private void NotifyAllSoundToggleButtons()
    {
        // Find all GlobalSoundToggle components in the scene
        GlobalSoundToggle[] soundToggles;

#if UNITY_2023_1_OR_NEWER
        soundToggles = FindObjectsByType<GlobalSoundToggle>(FindObjectsSortMode.None);
#else
        soundToggles = FindObjectsOfType<GlobalSoundToggle>();
#endif

        foreach (var toggle in soundToggles)
        {
            if (toggle != null)
            {
                toggle.UpdateVisualState(isGlobalSoundEnabled);
            }
        }

        if (showDebugLogs && soundToggles.Length > 0)
            Debug.Log($"🔘 Notified {soundToggles.Length} sound toggle buttons");
    }

    // Save sound settings
    private void SaveSoundSettings()
    {
        PlayerPrefs.SetInt("GlobalSoundEnabled", isGlobalSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (showDebugLogs)
            Debug.Log($"💾 Sound settings saved - Enabled: {isGlobalSoundEnabled}");
    }

    // Load sound settings
    private void LoadSoundSettings()
    {
        isGlobalSoundEnabled = PlayerPrefs.GetInt("GlobalSoundEnabled", 1) == 1;

        if (showDebugLogs)
            Debug.Log($"📁 Sound settings loaded - Enabled: {isGlobalSoundEnabled}");
    }

    // Public method to check sound state
    public bool IsGlobalSoundEnabled()
    {
        return isGlobalSoundEnabled;
    }
}