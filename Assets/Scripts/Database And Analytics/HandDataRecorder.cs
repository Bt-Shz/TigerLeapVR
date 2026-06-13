using UnityEngine;
using System.Collections.Generic;
using StretchSense.OSCBridge;
using System;

/// <summary>
/// Records hand data from HandStateManager throughout a game session
/// and sends it to Firebase when the session ends
/// </summary>
public class HandDataRecorder : MonoBehaviour
{
    public static HandDataRecorder Instance { get; private set; }
    
    [Header("Recording Settings")]
    [SerializeField] private float recordingInterval = 0.5f; // Record data every 500ms (to reduce size)
    [SerializeField] private bool recordJointData = false; // Skip joint data to reduce size (saves ~80% space)
    [SerializeField] private bool debugLogging = false;
    
    // Current session data
    private HandDataSession currentSession;
    private bool isRecording = false;
    private float nextRecordTime = 0f;
    private DateTime sessionStartTime;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (isRecording && Time.time >= nextRecordTime)
        {
            RecordCurrentHandData();
            nextRecordTime = Time.time + recordingInterval;
        }
    }
    
    /// <summary>
    /// Start recording hand data for a new game session
    /// </summary>
    /// <param name="gameType">Type of game (e.g., "Mahjong", "Taichi")</param>
    public void StartRecording(string gameType)
    {
        if (HandStateManager.Instance == null)
        {
            Debug.LogWarning("⚠️ HandStateManager not found, cannot record hand data");
            return;
        }
        
        sessionStartTime = DateTime.Now;
        currentSession = new HandDataSession
        {
            GameType = gameType,
            SessionStartTime = sessionStartTime,
            LeftHandSamples = new List<HandDataSample>(),
            RightHandSamples = new List<HandDataSample>()
        };
        
        isRecording = true;
        nextRecordTime = Time.time;
        
        if (debugLogging)
            Debug.Log($"🎮 Started recording hand data for {gameType}");
    }
    
    /// <summary>
    /// Stop recording and send data to Firebase
    /// </summary>
    public async void StopRecordingAndSend()
    {
        if (!isRecording || currentSession == null)
        {
            if (debugLogging)
                Debug.LogWarning("⚠️ No active recording session to stop");
            return;
        }
        
        isRecording = false;
        
        // Finalize session data
        currentSession.SessionEndTime = DateTime.Now;
        currentSession.SessionDuration = (float)(currentSession.SessionEndTime - sessionStartTime).TotalSeconds;
        
        // Calculate summary statistics
        currentSession.CalculateStatistics();
        
        if (debugLogging)
        {
            Debug.Log($"📊 Stopped recording. Duration: {currentSession.SessionDuration:F2}s, " +
                     $"Left hand samples: {currentSession.LeftHandSamples.Count}, " +
                     $"Right hand samples: {currentSession.RightHandSamples.Count}");
        }
        
        // Send to Firebase
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.isFirebaseInitialized)
        {
            await FirebaseManager.Instance.SaveHandDataSession(currentSession);
            
            if (debugLogging)
                Debug.Log("✅ Hand data session sent to Firebase");
        }
        else
        {
            Debug.LogWarning("⚠️ Firebase not available, hand data not saved");
        }
        
        // Clear current session
        currentSession = null;
    }
    
    /// <summary>
    /// Record current state of both hands
    /// </summary>
    private void RecordCurrentHandData()
    {
        if (HandStateManager.Instance == null || currentSession == null)
            return;
        
        float timestamp = (float)(DateTime.Now - sessionStartTime).TotalSeconds;
        
        // Record left hand data
        HandData leftHand = HandStateManager.Instance.leftHand;
        if (leftHand != null)
        {
            currentSession.LeftHandSamples.Add(CreateHandDataSample(leftHand, timestamp));
        }
        
        // Record right hand data
        HandData rightHand = HandStateManager.Instance.rightHand;
        if (rightHand != null)
        {
            currentSession.RightHandSamples.Add(CreateHandDataSample(rightHand, timestamp));
        }
    }
    
    /// <summary>
    /// Create a snapshot of hand data at the current moment
    /// </summary>
    private HandDataSample CreateHandDataSample(HandData handData, float timestamp)
    {
        var sample = new HandDataSample
        {
            Timestamp = timestamp,
            Accelerometer = new Vector3Data(handData.accelerometer),
            Orientation = new QuaternionData(handData.orientation),
            TrackerOffset = new Vector3Data(handData.trackerOffset),
            TrackerSource = handData.trackerSource,
            TrackerLocation = handData.trackerLocation,
            ButtonPassthroughEnabled = handData.buttonPassthroughEnabled
        };
        
        // Record joint data if available and enabled (joint data is very large)
        if (recordJointData && handData.joints != null && handData.joints.Count > 0)
        {
            sample.Joints = new List<JointDataSample>();
            foreach (var joint in handData.joints)
            {
                sample.Joints.Add(new JointDataSample
                {
                    Name = joint.name,
                    Position = new Vector3Data(joint.position),
                    Rotation = new QuaternionData(joint.rotation)
                });
            }
        }
        
        return sample;
    }
    
    /// <summary>
    /// Check if currently recording
    /// </summary>
    public bool IsRecording()
    {
        return isRecording;
    }
    
    /// <summary>
    /// Get current session data (useful for debugging)
    /// </summary>
    public HandDataSession GetCurrentSession()
    {
        return currentSession;
    }
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

/// <summary>
/// Represents a complete hand data recording session for a game
/// </summary>
[System.Serializable]
public class HandDataSession
{
    // Basic session info
    public string GameType;                          // "Mahjong" or "Taichi"
    public DateTime SessionStartTime;
    public DateTime SessionEndTime;
    public float SessionDuration;                    // In seconds
    public List<HandDataSample> LeftHandSamples;
    public List<HandDataSample> RightHandSamples;
    
    // Game metadata
    public string UserId;
    public string GameDifficulty;                    // "Easy", "Medium", "Hard", or "Normal"
    public bool GameCompleted;                       // Did the player complete/win?
    public int FinalScore;                           // Final score achieved
    
    // Common game statistics (both games)
    public int TotalAttempts;                        // Total attempts/actions taken
    public int FailedAttempts;                       // Failed attempts/misses
    public float TimeTaken;                          // Time taken to complete (or total time played)
    public float Accuracy;                           // Accuracy percentage (0-100)
    
    // Mahjong-specific fields
    public float TimeLimit;                          // Time limit for the game
    public int MatchesCompleted;                     // Number of matches completed
    public int TotalMatchesNeeded;                   // Total matches needed to win
    
    // Taichi-specific fields
    public int MaxCombo;                             // Maximum combo achieved
    public float MaxDelayBetweenActions;             // Maximum delay between actions
    public int TotalCubesSpawned;                    // Total cubes spawned

    // Food Choose-specific fields
    public float FinalOil;
    public float FinalSugar;
    public int TotalItemsPlaced;
    public string LossReason; // "Time", "Oil", "Sugar", or "None" (if won)


    // Summary statistics (calculated before sending to reduce storage size)
    public HandMovementStats LeftHandStats;
    public HandMovementStats RightHandStats;
    
    /// <summary>
    /// Calculate summary statistics from the recorded samples
    /// </summary>
    public void CalculateStatistics()
    {
        LeftHandStats = CalculateHandStats(LeftHandSamples);
        RightHandStats = CalculateHandStats(RightHandSamples);
    }
    
    private HandMovementStats CalculateHandStats(List<HandDataSample> samples)
    {
        if (samples == null || samples.Count == 0)
            return new HandMovementStats();
        
        var stats = new HandMovementStats
        {
            SampleCount = samples.Count,
            AverageAcceleration = Vector3.zero,
            MaxAcceleration = 0f,
            TotalMovementDistance = 0f
        };
        
        Vector3 sumAccel = Vector3.zero;
        Vector3 prevPos = Vector3.zero;
        bool firstSample = true;
        
        foreach (var sample in samples)
        {
            // Average acceleration
            Vector3 accel = sample.Accelerometer.ToVector3();
            sumAccel += accel;
            
            // Max acceleration magnitude
            float accelMag = accel.magnitude;
            if (accelMag > stats.MaxAcceleration)
                stats.MaxAcceleration = accelMag;
            
            // Calculate movement distance from tracker offset changes
            if (!firstSample)
            {
                Vector3 currentPos = sample.TrackerOffset.ToVector3();
                stats.TotalMovementDistance += Vector3.Distance(prevPos, currentPos);
            }
            
            prevPos = sample.TrackerOffset.ToVector3();
            firstSample = false;
        }
        
        if (samples.Count > 0)
        {
            stats.AverageAcceleration = sumAccel / samples.Count;
        }
        
        return stats;
    }
}

/// <summary>
/// A single timestamped sample of hand data
/// </summary>
[System.Serializable]
public class HandDataSample
{
    public float Timestamp;                          // Seconds since session start
    public Vector3Data Accelerometer;
    public QuaternionData Orientation;
    public Vector3Data TrackerOffset;
    public int TrackerSource;
    public int TrackerLocation;
    public bool ButtonPassthroughEnabled;
    public List<JointDataSample> Joints;
}

/// <summary>
/// Joint data snapshot (position, rotation, name)
/// </summary>
[System.Serializable]
public class JointDataSample
{
    public string Name;
    public Vector3Data Position;
    public QuaternionData Rotation;
}

/// <summary>
/// Serializable Vector3 (Unity Vector3 can't be directly serialized to Firebase)
/// </summary>
[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
    
    public Vector3Data() { }
    
    public Vector3Data(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

/// <summary>
/// Serializable Quaternion (Unity Quaternion can't be directly serialized to Firebase)
/// </summary>
[System.Serializable]
public class QuaternionData
{
    public float x;
    public float y;
    public float z;
    public float w;
    
    public QuaternionData() { }
    
    public QuaternionData(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
    
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}

/// <summary>
/// Summary statistics for hand movement during a session
/// </summary>
[System.Serializable]
public class HandMovementStats
{
    public int SampleCount;
    public Vector3 AverageAcceleration;
    public float MaxAcceleration;
    public float TotalMovementDistance;
}
