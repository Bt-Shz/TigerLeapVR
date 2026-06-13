using UnityEngine;
using MoreMountains.Tools;

public class UniversalSoundController : MonoBehaviour
{
    public static UniversalSoundController Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void SetGlobalSoundEnabled(bool enabled)
    {
        Debug.Log($"🌍 UniversalSoundController: Setting global sound to {enabled}");
        
        // Method 1: AudioListener (affects ALL Unity audio)
        AudioListener.volume = enabled ? 1f : 0f;
        
        // Method 2: AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.isGlobalSoundEnabled = enabled;
        }
        
        // Method 3: MMSoundManager (Feel system)
        ControlMMSoundManager(enabled);
        
        // Method 4: All AudioSources in scene
        ControlAllAudioSources(enabled);
        
        Debug.Log($"🔊 Global sound control complete - AudioListener.volume: {AudioListener.volume}");
    }
    
    private static void ControlMMSoundManager(bool enabled)
    {
        var mmSoundManager = Object.FindFirstObjectByType<MMSoundManager>();
        if (mmSoundManager != null)
        {
            if (enabled)
            {
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.UnmuteTrack, MMSoundManager.MMSoundManagerTracks.Master);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Master, 1f);
            }
            else
            {
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.MuteTrack, MMSoundManager.MMSoundManagerTracks.Master);
                MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Master, 0f);
            }
            Debug.Log($"🎵 MMSoundManager controlled: {enabled}");
        }
    }
    
    private static void ControlAllAudioSources(bool enabled)
    {
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource audioSource in allAudioSources)
        {
            audioSource.mute = !enabled;
        }
        Debug.Log($"🔊 Controlled {allAudioSources.Length} AudioSources");
    }
}