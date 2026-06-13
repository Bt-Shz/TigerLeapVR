using UnityEngine;
using StretchSense.OSCBridge;
using System.Collections.Generic;
using System.Linq;

public enum HandGestureType
{
    None,
    Fist,
    Open,
    Point
}

public class GestureInputManager : MonoBehaviour
{
    public static GestureInputManager Instance { get; private set; }

    [Header("Joint Names Configuration")]
    [Tooltip("Name of the wrist joint in the HandData")]
    public string wristName = "palm";
    public string thumbTipName = "thumb_tip";
    public string indexTipName = "index_tip";
    public string middleTipName = "middle_tip";
    public string ringTipName = "ring_tip";
    public string pinkyTipName = "pinky_tip";
    public string middleMcpName = "middle_mcp";

    [Header("Detection Settings")]
    [Tooltip("Distance from wrist to tip to consider a finger extended.")]
    public float extensionThreshold = 0.1f; 
    
    [Tooltip("If true, uses hand size (Wrist-MiddleMCP) to determine threshold dynamically.")]
    public bool useRelativeThreshold = true;
    [Tooltip("Multiplier for hand size to determine extension threshold (e.g. 1.5 means tip must be 1.5x further than knuckle).")]
    public float relativeThresholdMultiplier = 1.2f;

    [Tooltip("If true, allows for more 'sloppy' gestures (e.g. Open with 3 fingers, Fist with 1 non-index finger).")]
    public bool useRelaxedDetection = true;


    [Tooltip("Minimum distance from Wrist to Middle Knuckle. Filters out noise/empty hands.")]
    public float minHandSize = 0.01f; 
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    [Tooltip("Enable keyboard keys to simulate gestures (A/S/D for Left, F/G/H for Right)")]
    public bool enableKeyboardDebug = true;
    public HandGestureType debugLeftGesture;
    public HandGestureType debugRightGesture;

    public HandGestureType CurrentLeftGesture { get; private set; }
    public HandGestureType CurrentRightGesture { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // 1. Real Detection
        if (HandStateManager.Instance != null)
        {
            CurrentLeftGesture = DetectGesture(HandStateManager.Instance.leftHand, "Left");
            CurrentRightGesture = DetectGesture(HandStateManager.Instance.rightHand, "Right");
        }

        // 2. Keyboard Override
        if (enableKeyboardDebug)
        {
            // Left Hand: A=Fist, S=Open, D=Point
            if (Input.GetKey(KeyCode.A)) CurrentLeftGesture = HandGestureType.Fist;
            else if (Input.GetKey(KeyCode.S)) CurrentLeftGesture = HandGestureType.Open;
            else if (Input.GetKey(KeyCode.D)) CurrentLeftGesture = HandGestureType.Point;

            // Right Hand: F=Fist, G=Open, H=Point
            if (Input.GetKey(KeyCode.F)) CurrentRightGesture = HandGestureType.Fist;
            else if (Input.GetKey(KeyCode.G)) CurrentRightGesture = HandGestureType.Open;
            else if (Input.GetKey(KeyCode.H)) CurrentRightGesture = HandGestureType.Point;
        }
        
        debugLeftGesture = CurrentLeftGesture;
        debugRightGesture = CurrentRightGesture;
    }

    // --- FK Calculation Helpers ---
    private Dictionary<string, JointData> _jointMap = new Dictionary<string, JointData>();

    private Vector3 GetRelativePosition(HandData hand, string targetJointName)
    {
        if (hand == null || hand.joints == null) return Vector3.zero;

        // 1. Build Map
        _jointMap.Clear();
        foreach (var j in hand.joints) _jointMap[j.name] = j;

        if (!_jointMap.ContainsKey(targetJointName)) return Vector3.zero;

        // 2. Build Path from Wrist to Target
        List<string> path = new List<string>();
        string current = targetJointName;
        
        int safety = 0;
        while (current != wristName && !string.IsNullOrEmpty(current) && safety < 10)
        {
            path.Add(current);
            current = GetParentName(current);
            safety++;
        }
        path.Reverse(); 

        // 3. Calculate Forward Kinematics
        // Start at Wrist (0,0,0) with Identity Rotation
        Vector3 currentPos = Vector3.zero; 
        Quaternion currentRot = Quaternion.identity;

        foreach (var jointName in path)
        {
            if (_jointMap.TryGetValue(jointName, out JointData j))
            {
                currentPos += currentRot * j.position;
                currentRot *= j.rotation;
            }
        }

        return currentPos;
    }

    private string GetParentName(string name)
    {
        if (name == wristName) return "";
        if (name.EndsWith("_tip")) return name.Replace("_tip", "_dip");
        if (name.EndsWith("_dip")) 
        {
            if (name.ToLower().Contains("thumb")) return name.Replace("_dip", "_mcp");
            return name.Replace("_dip", "_pip");
        }
        if (name.EndsWith("_pip")) return name.Replace("_pip", "_mcp");
        if (name.EndsWith("_mcp")) return name.Replace("_mcp", "_cmc");
        if (name.EndsWith("_cmc")) return wristName;
        return "";
    }
    // -----------------------------

    private HandGestureType DetectGesture(HandData hand, string side)
    {
        if (hand == null || hand.joints == null || hand.joints.Count == 0) return HandGestureType.None;

        // Use FK to get position relative to wrist
        Vector3 GetPos(string name) => GetRelativePosition(hand, name);

        Vector3 wristPos = Vector3.zero; // In wrist space, wrist is at 0,0,0
        
        // Check Hand Size (Validity)
        Vector3 mcpPos = GetPos(middleMcpName);
        float handSize = Vector3.Distance(mcpPos, wristPos);
        if (handSize < minHandSize)
        {
            if (showDebugLogs && Time.frameCount % 60 == 0) 
                Debug.Log($"{side} Hand too small/noise (Size: {handSize:F4} < {minHandSize}). Ignoring.");
            return HandGestureType.None;
        }

        // Calculate distances
        float thumbDist = Vector3.Distance(GetPos(thumbTipName), wristPos);
        float indexDist = Vector3.Distance(GetPos(indexTipName), wristPos);
        float middleDist = Vector3.Distance(GetPos(middleTipName), wristPos);
        float ringDist = Vector3.Distance(GetPos(ringTipName), wristPos);
        float pinkyDist = Vector3.Distance(GetPos(pinkyTipName), wristPos);

        float currentThreshold = extensionThreshold;
        if (useRelativeThreshold)
        {
            // Use hand size (Wrist -> Middle MCP) as a baseline.
            // If tip is significantly further than MCP, it's extended.
            currentThreshold = handSize * relativeThresholdMultiplier;
        }

        if (showDebugLogs)
        {
            // Debug: Print calculated FK positions
            Vector3 iPos = GetPos(indexTipName);
            Debug.Log($"{side} FK Pos - Wrist: {wristPos}, IndexTip: {iPos} | Dist: {indexDist:F4} | ThumbDist: {thumbDist:F4}");
            
            // Debug: Print all available joint names and positions
            // Removed timer to ensure it prints
            /*
            if (hand.joints != null)
            {
                string jointDump = string.Join("\n", hand.joints.Select(j => $"  {j.name}: {j.position.ToString("F4")}"));
                Debug.Log($"{side} Hand Full Joint Dump:\n{jointDump}");
            }
            */

            Debug.Log($"{side} Hand Distances - Index: {indexDist:F3}, Middle: {middleDist:F3}, Ring: {ringDist:F3}, Pinky: {pinkyDist:F3} | Threshold: {currentThreshold:F3} (Relative: {useRelativeThreshold})");
        }

        bool indexExtended = indexDist > currentThreshold;
        bool middleExtended = middleDist > currentThreshold;
        bool ringExtended = ringDist > currentThreshold;
        bool pinkyExtended = pinkyDist > currentThreshold;
        
        // Logic
        int extendedCount = (indexExtended ? 1 : 0) + (middleExtended ? 1 : 0) + (ringExtended ? 1 : 0) + (pinkyExtended ? 1 : 0);

        if (showDebugLogs)
        {
             Debug.Log($"{side} Hand Logic - Extended: {extendedCount}");
        }

        if (useRelaxedDetection)
        {
             // Open: 3 or 4 fingers extended.
             // ALSO: If only 2 fingers are extended, but one is the Middle finger (e.g. Index+Middle),
             // we treat it as a "lazy" Open (Peace sign) instead of Point.
             if (extendedCount >= 3 || (extendedCount == 2 && middleExtended)) return HandGestureType.Open;
             
             // Point: Index extended.
             // Relaxed: Allow up to 2 fingers extended (Index + one other sloppy finger).
             // EXCEPTION: If that second finger is Middle, we treated it as Open above.
             if (indexExtended && extendedCount <= 2) return HandGestureType.Point;
             
             // Fist: 0 or 1 finger (if not index)
             // Allows for a "sloppy" fist where one finger (like pinky) isn't fully curled.
             if (extendedCount == 0 || (extendedCount == 1 && !indexExtended)) return HandGestureType.Fist;
        }
        else
        {
            // Strict (Original)
            if (extendedCount >= 4) return HandGestureType.Open;
            if (extendedCount == 0) return HandGestureType.Fist;
            if (indexExtended && extendedCount == 1) return HandGestureType.Point;
        }

        return HandGestureType.None;
    }
}
