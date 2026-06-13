using UnityEngine;

public class PtAudioManager : MonoBehaviour
{


    [Header("---Audio Source----")]
    [SerializeField] AudioSource MusicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("---Audio Source----")]

    public AudioClip background;
    public AudioClip Hit;

    public AudioClip Miss;

    public AudioClip GameOver;

    public AudioClip Scorebonus;

    public AudioClip ButtonClick;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MusicSource.clip = background;
        MusicSource.Play();
    }
 public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
