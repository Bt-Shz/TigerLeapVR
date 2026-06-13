using UnityEngine;
using MoreMountains.Feedbacks;

public class MMFSoundWrapper : MonoBehaviour
{
    public static void PlayMMFSound(MMF_Player feedbackPlayer)
    {
        if (AudioManager.Instance != null && AudioManager.Instance.IsGlobalSoundEnabled())
        {
            feedbackPlayer?.PlayFeedbacks();
        }
    }
    
    public static void PlayMMFSoundAtPosition(MMF_Player feedbackPlayer, Vector3 position)
    {
        if (AudioManager.Instance != null && AudioManager.Instance.IsGlobalSoundEnabled())
        {
            feedbackPlayer?.PlayFeedbacks(position);
        }
    }
}
