using UnityEngine;
using UnityEngine.UI;

public class MusicToggleButton : MonoBehaviour
{
    [Header("Button Sprites")]
    public Sprite musicOnSprite;   // Music enabled icon
    public Sprite musicOffSprite;  // Music disabled icon
    
    private Button musicButton;
    private Image buttonImage;
    
    void Start()
    {
        // Get button components
        musicButton = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        // Add click listener
        if (musicButton != null)
        {
            musicButton.onClick.AddListener(OnMusicButtonClicked);
        }
        
        // Update button appearance based on current music state
        UpdateButtonAppearance();
    }
    
    void OnMusicButtonClicked()
    {
        // Toggle music through MusicManager
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ToggleMusic();
            UpdateButtonAppearance();
        }
    }
    
    void UpdateButtonAppearance()
    {
        if (buttonImage == null || MusicManager.Instance == null)
            return;
            
        // Update sprite based on music state
        if (MusicManager.Instance.IsMusicEnabled())
        {
            buttonImage.sprite = musicOnSprite;
        }
        else
        {
            buttonImage.sprite = musicOffSprite;
        }
    }
    
    // Call this if music state changes from elsewhere
    void OnEnable()
    {
        UpdateButtonAppearance();
    }
}