using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class GateSpawnSettings
{
    [Header("Gate Prefab")]
    [Tooltip("The gate prefab to spawn")]
    public GameObject gatePrefab;
    
    [Header("Spawn Timing")]
    [Tooltip("Time interval between spawns for this gate (in seconds)")]
    public float spawnInterval = 5f;
    
    [Tooltip("Random variation in spawn timing (±seconds) - set to 0 for constant timing")]
    public float spawnVariation = 0f;
    
    [Tooltip("Use constant spawning (ignores variation and probability)")]
    public bool useConstantSpawning = false;
    
    [Header("Spawn Control")]
    [Tooltip("Probability of spawning this gate each interval (0-1) - ignored if constant spawning")]
    [Range(0f, 1f)]
    public float spawnProbability = 0.7f;
    
    [Tooltip("Whether this gate type is enabled for spawning")]
    public bool isEnabled = true;
    
    [Header("Position Settings")]
    [Tooltip("Possible X positions for this gate spawning (if empty, uses global positions)")]
    public float[] customSpawnXPositions;
    
    [Tooltip("Custom Y position for this gate (if 0, uses global Y position)")]
    public float customSpawnYPosition = 0f;
    
    [Header("Advanced")]
    [Tooltip("Maximum number of this gate type that can exist simultaneously")]
    public int maxSimultaneousGates = 2;
    
    [Tooltip("Delay before first spawn of this gate type (in seconds)")]
    public float initialDelay = 0f;
    
    // Internal tracking
    [System.NonSerialized]
    public float nextSpawnTime = 0f;
    [System.NonSerialized]
    public List<GameObject> activeGates = new List<GameObject>();
    [System.NonSerialized]
    public bool hasStarted = false;
}

public class PTGateSpawnManager : MonoBehaviour
{
    [Header("Individual Gate Settings")]
    [Tooltip("Individual spawn settings for each gate type")]
    public GateSpawnSettings[] gateSettings;
    
    [Header("Global Spawn Settings")]
    [Tooltip("Whether to start spawning gates automatically")]
    public bool autoStart = false;
    
    [Header("Global Spawn Positions")]
    [Tooltip("Default X positions for gate spawning (used when gate has no custom positions)")]
    public float[] globalSpawnXPositions = { -3f, 0f, 3f };
    
    [Tooltip("Default Y position for gate spawning")]
    public float globalSpawnYPosition = 1f;
    
    [Tooltip("Z position where gates spawn")]
    public float spawnZPosition = 180f;
    
    [Header("Global Spawning Control")]
    [Tooltip("Master enable/disable for all gate spawning")]
    public bool isSpawning = false;
    
    [Header("Legacy Support (Deprecated)")]
    [Tooltip("Legacy gate prefabs array - will be converted to new system")]
    public GameObject[] gatePrefabs;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool showDebug = false;
    
    [Tooltip("Show detailed per-gate debug information")]
    public bool showDetailedDebug = false;
    
    private Coroutine spawnCoroutine;
    
    void Start()
    {
        ConvertLegacySettings();
        
        if (autoStart)
        {
            StartSpawning();
        }
    }
    
    /// <summary>
    /// Converts legacy gatePrefabs array to new system
    /// </summary>
    private void ConvertLegacySettings()
    {
        if (gatePrefabs != null && gatePrefabs.Length > 0 && (gateSettings == null || gateSettings.Length == 0))
        {
            if (showDebug)
                Debug.Log("PTGateSpawnManager: Converting legacy gate prefabs to new system");
            
            gateSettings = new GateSpawnSettings[gatePrefabs.Length];
            
            for (int i = 0; i < gatePrefabs.Length; i++)
            {
                gateSettings[i] = new GateSpawnSettings();
                gateSettings[i].gatePrefab = gatePrefabs[i];
                gateSettings[i].spawnInterval = 5f;
                gateSettings[i].spawnVariation = 1f;
                gateSettings[i].spawnProbability = 0.7f;
                gateSettings[i].isEnabled = true;
                gateSettings[i].maxSimultaneousGates = 2;
            }
            
            // Clear legacy array to avoid confusion
            gatePrefabs = null;
        }
    }
    
    /// <summary>
    /// Starts the gate spawning process
    /// </summary>
    public void StartSpawning()
    {
        if (!isSpawning && gateSettings != null && gateSettings.Length > 0)
        {
            isSpawning = true;
            InitializeGateTimings();
            spawnCoroutine = StartCoroutine(SpawnGates());
            
            if (showDebug)
                Debug.Log("PTGateSpawnManager: Started spawning gates");
        }
    }
    
    /// <summary>
    /// Stops the gate spawning process
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
            
            // Reset all gate timings
            foreach (var settings in gateSettings)
            {
                if (settings != null)
                {
                    settings.hasStarted = false;
                    settings.nextSpawnTime = 0f;
                }
            }
            
            if (showDebug)
                Debug.Log("PTGateSpawnManager: Stopped spawning gates");
        }
    }
    
    /// <summary>
    /// Initializes spawn timings for all gate types
    /// </summary>
    private void InitializeGateTimings()
    {
        float currentTime = Time.time;
        
        foreach (var settings in gateSettings)
        {
            if (settings != null && settings.isEnabled)
            {
                // Set initial spawn time with delay
                settings.nextSpawnTime = currentTime + settings.initialDelay;
                settings.hasStarted = false;
                
                if (showDetailedDebug)
                {
                    Debug.Log($"PTGateSpawnManager: {settings.gatePrefab?.name} will first spawn at {settings.nextSpawnTime}");
                }
            }
        }
    }
    
    /// <summary>
    /// Main spawning coroutine
    /// </summary>
    private IEnumerator SpawnGates()
    {
        while (isSpawning)
        {
            float currentTime = Time.time;
            
            // Check each gate type individually
            foreach (var settings in gateSettings)
            {
                if (settings != null && settings.isEnabled && ShouldProcessGateType(settings, currentTime))
                {
                    ProcessGateSpawning(settings, currentTime);
                }
            }
            
            // Small delay to prevent excessive CPU usage
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// Checks if a gate type should be processed for spawning
    /// </summary>
    private bool ShouldProcessGateType(GateSpawnSettings settings, float currentTime)
    {
        return settings.gatePrefab != null && currentTime >= settings.nextSpawnTime;
    }
    
    /// <summary>
    /// Processes spawning for a specific gate type
    /// </summary>
    private void ProcessGateSpawning(GateSpawnSettings settings, float currentTime)
    {
        // Clean up destroyed gates
        CleanupDestroyedGates(settings);
        
        // Check if we should spawn this gate type
        if (ShouldSpawnGate(settings))
        {
            SpawnGate(settings);
        }
        
        // Calculate next spawn time
        CalculateNextSpawnTime(settings, currentTime);
    }
    
    /// <summary>
    /// Determines if a specific gate type should be spawned
    /// </summary>
    private bool ShouldSpawnGate(GateSpawnSettings settings)
    {
        // Check maximum simultaneous gates
        if (settings.activeGates.Count >= settings.maxSimultaneousGates)
        {
            if (showDetailedDebug)
                Debug.Log($"PTGateSpawnManager: {settings.gatePrefab.name} reached max simultaneous limit ({settings.maxSimultaneousGates})");
            return false;
        }
        
        // If using constant spawning, always spawn (ignoring probability)
        if (settings.useConstantSpawning)
        {
            return true;
        }
        
        // Check probability for non-constant spawning
        if (Random.Range(0f, 1f) > settings.spawnProbability)
        {
            if (showDetailedDebug)
                Debug.Log($"PTGateSpawnManager: {settings.gatePrefab.name} failed probability check");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Spawns a gate of the specified type
    /// </summary>
    private void SpawnGate(GateSpawnSettings settings)
    {
        // Determine spawn positions to use
        float[] spawnXPositions = settings.customSpawnXPositions != null && settings.customSpawnXPositions.Length > 0 
            ? settings.customSpawnXPositions 
            : globalSpawnXPositions;
        
        if (spawnXPositions.Length == 0)
        {
            if (showDebug)
                Debug.LogWarning($"PTGateSpawnManager: No spawn positions available for {settings.gatePrefab.name}");
            return;
        }
        
        // Pick random X position
        int randomXIndex = Random.Range(0, spawnXPositions.Length);
        float spawnX = spawnXPositions[randomXIndex];
        
        // Determine Y position
        float spawnY = settings.customSpawnYPosition > 0 ? settings.customSpawnYPosition : globalSpawnYPosition;
        
        // Create spawn position
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, spawnZPosition);
        
        // Instantiate the gate
        GameObject spawnedGate = Instantiate(settings.gatePrefab, spawnPosition, Quaternion.identity);
        
        // Add to tracking list
        settings.activeGates.Add(spawnedGate);
        
        if (showDebug)
        {
            string spawnType = settings.useConstantSpawning ? "CONSTANT" : "RANDOM";
            Debug.Log($"PTGateSpawnManager: Spawned {settings.gatePrefab.name} ({spawnType}) at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// Calculates the next spawn time for a gate type
    /// </summary>
    private void CalculateNextSpawnTime(GateSpawnSettings settings, float currentTime)
    {
        if (settings.useConstantSpawning)
        {
            // Constant spawning - fixed interval, no variation
            settings.nextSpawnTime = currentTime + settings.spawnInterval;
        }
        else
        {
            // Random spawning - with variation
            float variation = Random.Range(-settings.spawnVariation, settings.spawnVariation);
            settings.nextSpawnTime = currentTime + settings.spawnInterval + variation;
            settings.nextSpawnTime = Mathf.Max(settings.nextSpawnTime, currentTime + 0.1f); // Minimum delay
        }
        
        if (showDetailedDebug)
        {
            string spawnType = settings.useConstantSpawning ? "CONSTANT" : "RANDOM";
            Debug.Log($"PTGateSpawnManager: {settings.gatePrefab.name} ({spawnType}) next spawn at {settings.nextSpawnTime}");
        }
    }
    
    /// <summary>
    /// Removes destroyed gates from a specific gate type's tracking list
    /// </summary>
    private void CleanupDestroyedGates(GateSpawnSettings settings)
    {
        for (int i = settings.activeGates.Count - 1; i >= 0; i--)
        {
            if (settings.activeGates[i] == null)
            {
                settings.activeGates.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Destroys all active gates
    /// </summary>
    public void DestroyAllGates()
    {
        int totalDestroyed = 0;
        
        foreach (var settings in gateSettings)
        {
            if (settings != null)
            {
                foreach (GameObject gate in settings.activeGates)
                {
                    if (gate != null)
                    {
                        Destroy(gate);
                        totalDestroyed++;
                    }
                }
                settings.activeGates.Clear();
            }
        }
        
        if (showDebug && totalDestroyed > 0)
        {
            Debug.Log($"PTGateSpawnManager: Destroyed {totalDestroyed} gates");
        }
    }
    
    /// <summary>
    /// Gets the total number of currently active gates
    /// </summary>
    public int GetActiveGateCount()
    {
        int totalCount = 0;
        
        foreach (var settings in gateSettings)
        {
            if (settings != null)
            {
                CleanupDestroyedGates(settings);
                totalCount += settings.activeGates.Count;
            }
        }
        
        return totalCount;
    }
    
    /// <summary>
    /// Gets the number of active gates for a specific gate type
    /// </summary>
    public int GetActiveGateCount(GameObject gatePrefab)
    {
        foreach (var settings in gateSettings)
        {
            if (settings != null && settings.gatePrefab == gatePrefab)
            {
                CleanupDestroyedGates(settings);
                return settings.activeGates.Count;
            }
        }
        return 0;
    }
    
    /// <summary>
    /// Manually spawn a single gate of the first enabled type (for testing)
    /// </summary>
    [ContextMenu("Spawn Single Gate")]
    public void SpawnSingleGate()
    {
        foreach (var settings in gateSettings)
        {
            if (settings != null && settings.isEnabled && settings.gatePrefab != null)
            {
                SpawnGate(settings);
                break;
            }
        }
    }
    
    /// <summary>
    /// Spawn a specific gate type by index
    /// </summary>
    public void SpawnSpecificGate(int gateIndex)
    {
        if (gateIndex >= 0 && gateIndex < gateSettings.Length)
        {
            var settings = gateSettings[gateIndex];
            if (settings != null && settings.isEnabled && settings.gatePrefab != null)
            {
                SpawnGate(settings);
            }
        }
    }
    
    /// <summary>
    /// Enable/disable a specific gate type
    /// </summary>
    public void SetGateTypeEnabled(int gateIndex, bool enabled)
    {
        if (gateIndex >= 0 && gateIndex < gateSettings.Length && gateSettings[gateIndex] != null)
        {
            gateSettings[gateIndex].isEnabled = enabled;
            
            if (showDebug)
            {
                Debug.Log($"PTGateSpawnManager: Gate type {gateIndex} ({gateSettings[gateIndex].gatePrefab?.name}) set to {(enabled ? "enabled" : "disabled")}");
            }
        }
    }
    
    /// <summary>
    /// Toggle gate spawning on/off
    /// </summary>
    [ContextMenu("Toggle Gate Spawning")]
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
        // Validate all gate settings
        if (gateSettings != null)
        {
            foreach (var settings in gateSettings)
            {
                if (settings != null)
                {
                    // Ensure spawn interval is not negative
                    if (settings.spawnInterval < 0f)
                    {
                        settings.spawnInterval = 0f;
                    }
                    
                    // Ensure spawn variation is not negative
                    if (settings.spawnVariation < 0f)
                    {
                        settings.spawnVariation = 0f;
                    }
                    
                    // Ensure spawn probability is between 0 and 1
                    settings.spawnProbability = Mathf.Clamp01(settings.spawnProbability);
                    
                    // Ensure max simultaneous gates is at least 1
                    if (settings.maxSimultaneousGates < 1)
                    {
                        settings.maxSimultaneousGates = 1;
                    }
                    
                    // Ensure initial delay is not negative
                    if (settings.initialDelay < 0f)
                    {
                        settings.initialDelay = 0f;
                    }
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw global spawn positions
        if (globalSpawnXPositions != null)
        {
            Gizmos.color = Color.blue;
            foreach (float xPos in globalSpawnXPositions)
            {
                Vector3 gizmoPosition = new Vector3(xPos, globalSpawnYPosition, spawnZPosition);
                Gizmos.DrawWireCube(gizmoPosition, Vector3.one * 2f);
                Gizmos.DrawLine(gizmoPosition, new Vector3(xPos, globalSpawnYPosition, -7f));
            }
        }
        
        // Draw custom spawn positions for each gate type
        if (gateSettings != null)
        {
            Color[] gateColors = { Color.red, Color.green, Color.yellow, Color.magenta, Color.cyan };
            
            for (int i = 0; i < gateSettings.Length; i++)
            {
                var settings = gateSettings[i];
                if (settings != null && settings.customSpawnXPositions != null && settings.customSpawnXPositions.Length > 0)
                {
                    Gizmos.color = gateColors[i % gateColors.Length];
                    float yPos = settings.customSpawnYPosition > 0 ? settings.customSpawnYPosition : globalSpawnYPosition;
                    
                    foreach (float xPos in settings.customSpawnXPositions)
                    {
                        Vector3 gizmoPosition = new Vector3(xPos, yPos, spawnZPosition);
                        Gizmos.DrawWireSphere(gizmoPosition, 1.5f);
                        Gizmos.DrawLine(gizmoPosition, new Vector3(xPos, yPos, -7f));
                    }
                }
            }
        }
        
        // Draw the movement path
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(0, globalSpawnYPosition, spawnZPosition), new Vector3(0, globalSpawnYPosition, -7f));
    }
}