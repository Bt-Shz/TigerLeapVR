using UnityEngine;
using UnityEngine.UI;

public class GestureZone : MonoBehaviour
{
    [Header("Settings")]
    public bool isLeftHandZone;
    public float detectionRadius = 1.5f;
    
    [Header("Feedback")]
    public Transform feedbackPosition;
    public GameObject niceFeedbackPrefab;
    public GameObject missFeedbackPrefab;

    [Header("VFX")]
    public StarBurst starBurstVFX;
    
    [Header("Debug")]
    public bool showDebug = true;

    void Update()
    {
        // Check for notes in range
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        
        if (showDebug && hits.Length > 0)
        {
             // Debug.Log($"Zone {name} hit {hits.Length} objects");
        }

        foreach(var hit in hits)
        {
            // Try getting component on object or parent
            GestureNoteMover note = hit.GetComponentInParent<GestureNoteMover>();
            
            if(note != null)
            {
                if (note.isLeftHand == isLeftHandZone)
                {
                    CheckInput(note);
                }
                else if (showDebug)
                {
                    // Debug.Log($"Zone {name} ignored {note.name} (Hand Mismatch)");
                }
            }
            else if (showDebug)
            {
                 // Debug.Log($"Zone {name} hit {hit.name} but no GestureNoteMover found");
            }
        }
    }
    
    void CheckInput(GestureNoteMover note)
    {
        if (GestureInputManager.Instance == null) return;

        // Get Input
        HandGestureType currentGesture = isLeftHandZone ? 
            GestureInputManager.Instance.CurrentLeftGesture : 
            GestureInputManager.Instance.CurrentRightGesture;
            
        if (showDebug)
        {
            // Debug.Log($"Zone {name} sees {note.gestureType}. Player doing: {currentGesture}");
        }

        // Check if gesture matches
        if(currentGesture == note.gestureType)
        {
            // Success
            Vector3 hitPosition = note.transform.position;
            note.OnCaught();
            ShowFeedback(true);
            
            if (starBurstVFX != null)
            {
                starBurstVFX.Play(hitPosition);
            }

            if (PTScoreManager.Instance != null) PTScoreManager.Instance.OnCubeCaught();
        }
        else
        {
            // Check if it passed the center of the zone (missed)
            // Assuming moving along Z, and Zone is at some Z.
            // If note.z < zone.z - threshold, it's a miss.
            if(note.transform.position.z < transform.position.z - 0.5f)
            {
                 note.OnCaught(); // Destroy it so we don't trigger miss multiple times
                 // ShowFeedback(false);
                 if (PTGameManager.Instance != null) PTGameManager.Instance.NotifyMiss();
            }
        }
    }
    
    void ShowFeedback(bool success)
    {
        GameObject prefab = success ? niceFeedbackPrefab : missFeedbackPrefab;
        if (prefab != null && feedbackPosition != null)
        {
            Instantiate(prefab, feedbackPosition.position, Quaternion.identity);
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = isLeftHandZone ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
