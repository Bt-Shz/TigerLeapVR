using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip backgroundMusic;
    private AudioSource musicSource;
    
    [Header("Music Control")]
    public bool isMusicEnabled = true;
    public float musicVolume = 0.5f;
    
    // Singleton pattern
    public static MusicManager Instance;
    
    void Awake()
    {
        // Singleton setup - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            InitializeMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeMusic()
    {
        // Get or add AudioSource component
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
            
        // Configure AudioSource for background music
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
        
        // Load saved music preference
        isMusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        musicSource.volume = musicVolume;
        
        // Start playing if enabled
        if (isMusicEnabled && backgroundMusic != null)
        {
            musicSource.Play();
        }
    }
    
    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;
        
        if (isMusicEnabled)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
            musicSource.volume = musicVolume;
        }
        else
        {
            musicSource.volume = 0f;
        }
        
        // Save preference
        PlayerPrefs.SetInt("MusicEnabled", isMusicEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (isMusicEnabled)
        {
            musicSource.volume = musicVolume;
        }
        
        // Save preference
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }
    
    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }
}