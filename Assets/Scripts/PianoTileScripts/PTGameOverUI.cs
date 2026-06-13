using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PTGameOverUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI detailText;
    public Button playAgainButton;
    public Button quitButton;

    [Header("Text Defaults")]
    public string title = "Game Over";
    public string scoreLabel = "Score";
    public string detailFormat = "Misses: {0}\nMax Delay: {1:0.00}s";

    public System.Action onPlayAgain;
    public System.Action onQuit;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(() => onPlayAgain?.Invoke());
        if (quitButton != null) quitButton.onClick.AddListener(() => onQuit?.Invoke());
    }

    public void Show(int score, int misses, float maxDelay)
    {
        if (titleText != null) titleText.text = title;
        if (scoreText != null) scoreText.text = $"{scoreLabel}: {score}";
        if (detailText != null) detailText.text = string.Format(detailFormat, misses, maxDelay);
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
