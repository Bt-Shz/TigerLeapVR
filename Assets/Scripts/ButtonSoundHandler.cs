using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonSoundHandler : MonoBehaviour
{
    [Header("Button Sound Settings")]
    public AudioClip customButtonSound;  // Optional: Custom sound for this specific button
    public float volume = 0.7f;
    
    private Button button;
    private static GameObject persistentAudioPlayer;
    private static AudioSource persistentAudioSource;
    
    void Start()
    {
        // Get the button component
        button = GetComponent<Button>();
        
        if (button != null)
        {
            // Add click listener
            button.onClick.AddListener(PlayButtonSound);
        }
        
        // Ensure we have a persistent audio player
        EnsurePersistentAudioPlayer();
    }
    
    /// <summary>
    /// Creates or finds the persistent audio player that won't be destroyed
    /// </summary>
    private static void EnsurePersistentAudioPlayer()
    {
        if (persistentAudioPlayer == null)
        {
            // Create a persistent audio player
            persistentAudioPlayer = new GameObject("PersistentButtonAudioPlayer");
            persistentAudioSource = persistentAudioPlayer.AddComponent<AudioSource>();
            persistentAudioSource.playOnAwake = false;
            
            // Make it persistent across scene changes and object destruction
            DontDestroyOnLoad(persistentAudioPlayer);
            
            Debug.Log("ButtonSoundHandler: Created persistent audio player");
        }
    }
    
    void PlayButtonSound()
    {
        // Check PersistentSoundManager first
        bool soundEnabled = true;
        if (PersistentSoundManager.Instance != null)
        {
            soundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
        }
        
        if (!soundEnabled) return;
        
        // Ensure persistent audio player exists
        EnsurePersistentAudioPlayer();
        
        // Use custom sound if available
        if (customButtonSound != null)
        {
            PlaySoundPersistent(customButtonSound, volume);
        }
        else if (AudioManager.Instance != null)
        {
            // Try to get default button sound from AudioManager
            AudioManager.Instance.PlayButtonClick();
        }
        else
        {
            // Fallback: play a default sound if available
            Debug.LogWarning("ButtonSoundHandler: No audio clip assigned and no AudioManager found");
        }
    }
    
    /// <summary>
    /// Plays a sound using the persistent audio source
    /// </summary>
    private static void PlaySoundPersistent(AudioClip clip, float volumeLevel)
    {
        if (persistentAudioSource != null && clip != null)
        {
            persistentAudioSource.volume = volumeLevel;
            persistentAudioSource.PlayOneShot(clip);
            
            // Optional: Start coroutine to manage multiple sounds
            if (persistentAudioPlayer != null)
            {
                persistentAudioPlayer.GetComponent<MonoBehaviour>()?.StartCoroutine(ManageAudioPlayback(clip.length));
            }
        }
    }
    
    /// <summary>
    /// Manages audio playback to ensure it completes
    /// </summary>
    private static IEnumerator ManageAudioPlayback(float duration)
    {
        // Wait for the audio to finish
        yield return new WaitForSeconds(duration);
        
        // Audio has finished playing
        // You can add any cleanup logic here if needed
    }
    
    /// <summary>
    /// Alternative method: Play sound that's guaranteed to complete
    /// </summary>
    public void PlayButtonSoundGuaranteed()
    {
        StartCoroutine(PlaySoundCoroutine());
    }
    
    private IEnumerator PlaySoundCoroutine()
    {
        // Check sound settings
        bool soundEnabled = true;
        if (PersistentSoundManager.Instance != null)
        {
            soundEnabled = PersistentSoundManager.Instance.IsGlobalSoundEnabled();
        }
        
        if (!soundEnabled) yield break;
        
        // Ensure persistent audio player exists
        EnsurePersistentAudioPlayer();
        
        if (customButtonSound != null && persistentAudioSource != null)
        {
            persistentAudioSource.volume = volume;
            persistentAudioSource.PlayOneShot(customButtonSound);
            
            // Wait for the full duration of the sound
            yield return new WaitForSeconds(customButtonSound.length);
        }
    }
    
    void OnDestroy()
    {
        // Clean up listener
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonSound);
        }
    }
    
    /// <summary>
    /// Clean up persistent audio player when application quits
    /// </summary>
    void OnApplicationQuit()
    {
        if (persistentAudioPlayer != null)
        {
            Destroy(persistentAudioPlayer);
        }
    }
}