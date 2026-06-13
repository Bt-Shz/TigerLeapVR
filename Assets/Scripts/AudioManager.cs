using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.5f, 1.5f)]
        public float pitch = 1f;
        [HideInInspector]
        public AudioSource source;
        public bool loop = false;
    }
    
    // Singleton pattern
    public static AudioManager Instance;
    
    [Header("Audio Settings")]
    public Sound[] sounds;
    
    [Header("Global Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    public bool isMuted = false;
    public bool isGlobalSoundEnabled = true;  // NEW: Global sound toggle

    // NEW: Add button click sound
    [Header("UI Sounds")]
    public AudioClip buttonClickSound;
    private AudioSource uiAudioSource;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Simple initialization
            InitializeAudioSources();
            
            Debug.Log("🎵 AudioManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Sync with PersistentSoundManager if it exists
        SyncWithPersistentManager();
    }

    // NEW: Sync with PersistentSoundManager
    private void SyncWithPersistentManager()
    {
        if (PersistentSoundManager.Instance != null)
        {
            isGlobalSoundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
            Debug.Log($"🔗 AudioManager synced with PersistentSoundManager - Sound enabled: {isGlobalSoundEnabled}");
        }
    }

    // NEW: Separate audio source initialization
    private void InitializeAudioSources()
    {
        // Create audio sources for each sound
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume * masterVolume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
        
        // Create dedicated UI audio source
        if (buttonClickSound != null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.clip = buttonClickSound;
            uiAudioSource.volume = 0.7f * masterVolume;
            uiAudioSource.pitch = 1f;
            uiAudioSource.loop = false;
            
            Debug.Log("🎵 AudioManager: UI Audio source created");
        }
        
        // Apply initial sound settings
        ApplySoundSettings();
    }
    
    // NEW: Apply sound settings to ALL audio sources and systems
    private void ApplySoundSettings()
    {
        Debug.Log($"🎵 Applying sound settings - Enabled: {isGlobalSoundEnabled}");
        
        // Method 1: Control AudioListener (affects ALL Unity audio)
        AudioListener.volume = isGlobalSoundEnabled ? masterVolume : 0f;
        
        // Method 2: Update AudioManager sounds
        float effectiveVolume = isGlobalSoundEnabled ? masterVolume : 0f;
        
        foreach (Sound s in sounds)
        {
            if (s.source != null)
            {
                s.source.volume = effectiveVolume * s.volume;
            }
        }
        
        // Update UI audio source
        if (uiAudioSource != null)
        {
            uiAudioSource.volume = effectiveVolume * 0.7f;
        }
        
        // Method 3: Control MMSoundManager (Feel system)
        ControlMMSoundManager();
        
        Debug.Log($"🔊 AudioListener.volume: {AudioListener.volume}");
        Debug.Log($"🔊 Effective volume: {effectiveVolume}");
    }
    
    // NEW: Control MMSoundManager from Feel system
    private void ControlMMSoundManager()
    {
        // Find MMSoundManager in scene and control it
        var mmSoundManager = Object.FindFirstObjectByType<MoreMountains.Tools.MMSoundManager>();
        if (mmSoundManager != null)
        {
            if (isGlobalSoundEnabled)
            {
                // Unmute all tracks
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.UnmuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.Master);
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.UnmuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.Music);
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.UnmuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.Sfx);
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.UnmuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.UI);
            }
            else
            {
                // Mute all tracks
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.MuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.Master);
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.MuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.Music);
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.MuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.Sfx);
                MoreMountains.Tools.MMSoundManagerTrackEvent.Trigger(MoreMountains.Tools.MMSoundManagerTrackEventTypes.MuteTrack, MoreMountains.Tools.MMSoundManager.MMSoundManagerTracks.UI);
            }
            
            Debug.Log($"🎵 MMSoundManager tracks {(isGlobalSoundEnabled ? "unmuted" : "muted")}");
        }
    }
    
    // NEW: Save sound settings using PlayerPrefs
    private void SaveSoundSettings()
    {
        PlayerPrefs.SetInt("GlobalSoundEnabled", isGlobalSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // NEW: Load sound settings from PlayerPrefs
    private void LoadSoundSettings()
    {
        isGlobalSoundEnabled = PlayerPrefs.GetInt("GlobalSoundEnabled", 1) == 1;
    }
    
    // NEW: Get current sound state
    public bool IsGlobalSoundEnabled()
    {
        return isGlobalSoundEnabled;
    }
    
    // Play a sound by name
    public void Play(string name)
    {
        // Check both local and persistent manager
        bool soundEnabled = isGlobalSoundEnabled;
        if (PersistentSoundManager.Instance != null)
        {
            soundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
        }
        
        if (!soundEnabled) return;
        
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null)
        {
            s.source.Play();
        }
        else
        {
            Debug.LogWarning("Sound: " + name + " not found!");
        }
    }
    
    // Stop a sound by name
    public void Stop(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null)
        {
            s.source.Stop();
        }
        else
        {
            Debug.LogWarning("Sound: " + name + " not found!");
        }
    }
    
    // Stop all sounds
    public void StopAll()
    {
        foreach (Sound s in sounds)
        {
            if (s.source != null)
                s.source.Stop();
        }
    }
    
    // Check if a sound is playing
    public bool IsPlaying(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null)
        {
            return s.source.isPlaying;
        }
        return false;
    }
    
    // NEW: Play button click sound
    public void PlayButtonClick()
    {
        // Check both local and persistent manager
        bool soundEnabled = isGlobalSoundEnabled;
        if (PersistentSoundManager.Instance != null)
        {
            soundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
        }
        
        if (!soundEnabled || uiAudioSource == null) return;
        
        uiAudioSource.volume = 0.7f * masterVolume;
        uiAudioSource.PlayOneShot(buttonClickSound);
    }
    
    // Update existing SetVolume method to include UI sounds
    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        foreach (Sound s in sounds)
        {
            if (s.source != null)
                s.source.volume = s.volume * masterVolume;
        }
        
        // NEW: Update UI audio source volume
        if (uiAudioSource != null)
        {
            uiAudioSource.volume = 0.7f * masterVolume;
        }
    }
    
    // Update existing ToggleMute method to include UI sounds
    public void ToggleMute()
    {
        isMuted = !isMuted;
        
        foreach (Sound s in sounds)
        {
            if (s.source != null)
                s.source.volume = isMuted ? 0 : s.volume * masterVolume;
        }
        
        // NEW: Update UI audio source mute state
        if (uiAudioSource != null)
        {
            uiAudioSource.volume = isMuted ? 0 : 0.7f * masterVolume;
        }
    }
    
    // NEW: Debug method to test button sound
    [ContextMenu("Test Button Sound")]
    public void TestButtonSound()
    {
        Debug.Log("Testing button sound...");
        Debug.Log($"AudioManager Instance: {Instance != null}");
        Debug.Log($"Is Muted: {isMuted}");
        Debug.Log($"UI Audio Source: {uiAudioSource != null}");
        Debug.Log($"Button Click Sound: {buttonClickSound != null}");
        
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
            Debug.Log("Sound should play now!");
        }
        else
        {
            Debug.LogError("Missing components for button sound!");
        }
    }
}
