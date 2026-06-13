using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float destroyTime = 1f;

    void Start()
    {
        // Destroy the popup after a short time
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // Move the text UP every frame
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}