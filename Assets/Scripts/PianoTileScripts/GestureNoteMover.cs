using UnityEngine;

public class GestureNoteMover : MonoBehaviour
{
    [Header("Gesture Settings")]
    public HandGestureType gestureType;
    public bool isLeftHand;
    
    [Header("Movement")]
    public float speed = 15f;
    public float speedIncreaseRate = 0.5f;
    
    [Header("Visuals")]
    public SpriteRenderer iconRenderer;
    
    private bool hasBeenCaught = false;

    void Update()
    {
        // Increase speed over time
        speed += speedIncreaseRate * Time.deltaTime;
        
        // Move towards camera (assuming camera is at -Z, so move in -Z direction)
        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
        
        // Failsafe destruction
        if (transform.position.z < -10f) 
        {
            Destroy(gameObject);
        }
    }
    
    public void OnCaught()
    {
        if (hasBeenCaught) return;
        hasBeenCaught = true;
        Destroy(gameObject);
    }
}
