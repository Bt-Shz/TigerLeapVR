using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PTSpawnManager : MonoBehaviour
{
    [Header("Cube Spawning")]
    [Tooltip("The cube prefab to spawn (Legacy)")]
    public GameObject cubePrefab;

    [System.Serializable]
    public struct GesturePrefab
    {
        public HandGestureType gestureType;
        public GameObject prefab;
    }

    [Header("Gesture Prefabs")]
    public List<GesturePrefab> leftHandPrefabs;
    public List<GesturePrefab> rightHandPrefabs;

    [Header("Gesture Icons")]
    public Sprite leftFistIcon;
    public Sprite leftOpenIcon;
    public Sprite leftPointIcon;
    public Sprite rightFistIcon;
    public Sprite rightOpenIcon;
    public Sprite rightPointIcon;
    
    [Header("Spawn Settings")]
    [Tooltip("Time interval between spawns (in seconds)")]
    public float spawnInterval = 2f;
    
    [Tooltip("Whether to start spawning automatically")]
    public bool autoStart = false;
    
    [Header("Spawn Positions")]
    [Tooltip("Predefined spawn positions for the cubes (Left and Right tracks)")]
    public Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-2f, 0.5f, 200f), // Left Track
        new Vector3(2f, 0.5f, 200f)   // Right Track
    };

    [Tooltip("Predefined spawn rotations for the cubes (in Euler angles)")]
    public Vector3[] spawnRotations = new Vector3[]
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0f, 0f)
    };
    
    [Header("Gesture Settings")]
    public float noteSpeed = -15f;
    public float speedIncreaseRate = -0.5f;
    
    [Header("Spawning Control")]
    [Tooltip("Enable/disable spawning at runtime")]
    public bool isSpawning = false;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool showDebug = false;
    
    private Coroutine spawnCoroutine;
    
    // Singleton for easy access
    public static PTSpawnManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Menu manager controls spawning
    }
    
    /// <summary>
    /// Starts the spawning process
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning)
        {
            return;
        }

        if (!HasValidSpawnConfiguration())
        {
            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCubes());

        if (showDebug)
            Debug.Log("PTSpawnManager: Started spawning cubes");
    }
    
    /// <summary>
    /// Stops the spawning process
    /// </summary>
    public void StopSpawning()
    {
        if (isSpawning)
        {
            isSpawning = false;
            
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            if (showDebug)
                Debug.Log("PTSpawnManager: Stopped spawning cubes");
        }
    }
    
    /// <summary>
    /// Coroutine that handles the spawning logic
    /// </summary>
    private IEnumerator SpawnCubes()
    {
        while (isSpawning)
        {
            SpawnRandomCube();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    /// <summary>
    /// Spawns a cube at a random position from the predefined positions
    /// </summary>
    private void SpawnRandomCube()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            if (showDebug)
                Debug.LogWarning("PTSpawnManager: Cannot spawn cube - missing spawn positions");
            return;
        }
        
        // Pick a random spawn position (0 = Left, 1 = Right)
        int randomIndex = Random.Range(0, spawnPositions.Length);
        bool isLeft = (randomIndex == 0);
        Vector3 spawnPosition = spawnPositions[randomIndex];
        Vector3 spawnRotation = spawnRotations != null && randomIndex < spawnRotations.Length
            ? spawnRotations[randomIndex]
            : Vector3.zero;
        
        // Pick Random Gesture
        HandGestureType[] gestures = { HandGestureType.Fist, HandGestureType.Open, HandGestureType.Point };
        HandGestureType randomGesture = gestures[Random.Range(0, gestures.Length)];

        // Find Prefab
        GameObject prefabToSpawn = cubePrefab; // Fallback
        List<GesturePrefab> targetList = isLeft ? leftHandPrefabs : rightHandPrefabs;
        
        if (targetList != null)
        {
            foreach (var gp in targetList)
            {
                if (gp.gestureType == randomGesture && gp.prefab != null)
                {
                    prefabToSpawn = gp.prefab;
                    break;
                }
            }
        }

        if (prefabToSpawn == null) return;

        // Instantiate the cube
        GameObject spawnedCube = Instantiate(prefabToSpawn, spawnPosition, Quaternion.Euler(spawnRotation));
        spawnedCube.name = $"EasyHand_{(isLeft ? "Left" : "Right")}_{randomGesture}";
        
        // Setup Gesture Note (New System)
        GestureNoteMover noteMover = spawnedCube.GetComponent<GestureNoteMover>();
        if (noteMover == null)
        {
            Debug.LogError($"EasyHand prefab '{prefabToSpawn.name}' has no GestureNoteMover.");
            Destroy(spawnedCube);
            return;
        }

        SpriteRenderer iconRenderer = noteMover.iconRenderer != null
            ? noteMover.iconRenderer
            : spawnedCube.GetComponentInChildren<SpriteRenderer>();
        Sprite gestureIcon = GetGestureIcon(randomGesture, isLeft);
        if (iconRenderer == null || gestureIcon == null)
        {
            Debug.LogError(
                $"EasyHand cannot render {randomGesture} for the {(isLeft ? "left" : "right")} hand.");
            Destroy(spawnedCube);
            return;
        }

        iconRenderer.sprite = gestureIcon;
        noteMover.iconRenderer = iconRenderer;
        noteMover.gestureType = randomGesture;
        noteMover.isLeftHand = isLeft;
        noteMover.speed = Mathf.Abs(noteSpeed); // Ensure positive speed for Translate(back)
        noteMover.speedIncreaseRate = Mathf.Abs(speedIncreaseRate);
        
        if (showDebug)
        {
            Debug.Log($"PTSpawnManager: Spawned gesture {randomGesture} at position {spawnPosition} (Left: {isLeft})");
        }
    }

    private bool HasValidSpawnConfiguration()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("EasyHand cannot start because no note spawn positions are configured.");
            return false;
        }

        if (cubePrefab == null)
        {
            Debug.LogError("EasyHand cannot start because its note prefab is unassigned.");
            return false;
        }

        if (cubePrefab.GetComponent<GestureNoteMover>() == null)
        {
            Debug.LogError("EasyHand cannot start because its note prefab has no GestureNoteMover.");
            return false;
        }

        if (leftFistIcon == null || leftOpenIcon == null || leftPointIcon == null ||
            rightFistIcon == null || rightOpenIcon == null || rightPointIcon == null)
        {
            Debug.LogError("EasyHand cannot start because one or more gesture icons are unassigned.");
            return false;
        }

        return true;
    }

    private Sprite GetGestureIcon(HandGestureType gesture, bool isLeft)
    {
        if (isLeft)
        {
            switch (gesture)
            {
                case HandGestureType.Fist: return leftFistIcon;
                case HandGestureType.Open: return leftOpenIcon;
                case HandGestureType.Point: return leftPointIcon;
                default: return null;
            }
        }

        switch (gesture)
        {
            case HandGestureType.Fist: return rightFistIcon;
            case HandGestureType.Open: return rightOpenIcon;
            case HandGestureType.Point: return rightPointIcon;
            default: return null;
        }
    }
    
    /// <summary>
    /// Spawns a single cube manually (useful for testing)
    /// </summary>
    [ContextMenu("Spawn Single Cube")]
    public void SpawnSingleCube()
    {
        if (!HasValidSpawnConfiguration())
        {
            return;
        }

        SpawnRandomCube();
    }
    
    /// <summary>
    /// Toggle spawning on/off
    /// </summary>
    [ContextMenu("Toggle Spawning")]
    public void ToggleSpawning()
    {
        if (isSpawning)
        {
            StopSpawning();
        }
        else
        {
            StartSpawning();
        }
    }
    
    void OnValidate()
    {
        // Ensure spawn interval is not negative
        if (spawnInterval < 0f)
        {
            spawnInterval = 0f;
        }

        // Ensure spawnRotations array matches spawnPositions length
        if (spawnPositions != null && spawnRotations != null && spawnRotations.Length != spawnPositions.Length)
        {
            System.Array.Resize(ref spawnRotations, spawnPositions.Length);
            Debug.LogWarning("PTSpawnManager: Resized spawnRotations array to match spawnPositions length");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw spawn positions in scene view
        if (spawnPositions != null)
        {
            // Use different colors for each spawn position
            Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow };
            
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                Gizmos.color = colors[i % colors.Length];
                Gizmos.DrawWireCube(spawnPositions[i], Vector3.one);
                Gizmos.DrawWireSphere(spawnPositions[i], 0.5f);
                
                // Draw line to show movement path
                Gizmos.DrawLine(spawnPositions[i], new Vector3(spawnPositions[i].x, spawnPositions[i].y, -7f));
            }
        }
    }
}
