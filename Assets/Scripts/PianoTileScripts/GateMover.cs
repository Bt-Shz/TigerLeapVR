using UnityEngine;

public class GateMover : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("Initial speed of cube movement (negative means moving -Z)")]
    public float moveSpeed = -15f;

    [Tooltip("How much speed increases per second")]
    public float speedIncreaseRate = -0.5f; // negative keeps moving forward

    private float currentSpeed;

    [Header("Destruction")]
    [Tooltip("Z position at which the cube will be destroyed")]
    public float destructionZPosition = -7f;

    [Tooltip("Show debug messages when cube is destroyed")]
    public bool showDestructionDebug = false;

    void Start()
    {
        // Ensure the gate has the correct tag for identification
        if (!gameObject.CompareTag("DecorativeGate"))
        {
            gameObject.tag = "DecorativeGate";
        }
        currentSpeed = moveSpeed;

    }

    void Update()
    {
        currentSpeed += speedIncreaseRate * Time.deltaTime;

        // Move towards negative Z direction
        transform.position += (Vector3.forward * moveSpeed * Time.deltaTime);

        // Check if gate has moved beyond the destruction point
        if (transform.position.z <= destructionZPosition)
        {
            if (showDestructionDebug)
            {
                Debug.Log($"GateMover: Destroying gate at position {transform.position}");
            }

            Destroy(gameObject);
        }
    }
}