using UnityEngine;
using System.Collections;
using MoreMountains.Feedbacks;

public class CardController : MonoBehaviour
{
    private bool isFlipped = false;
    private bool isBusy = false;
    private Quaternion faceRotation;
    private Quaternion backRotation;
    private Collider col;
    private bool isMatched = false;

    [HideInInspector]
    public int cardTypeId = -1;

    // Feel Animation Components
    [Header("Feel Animation")]
    private MMF_Player flipFeedbackPlayer;
    private MMF_RotationSpring rotationSpring;
    private bool isFeelSetupComplete = false;

    // Static flag to ensure the countdown is only triggered once
    private static bool countdownStarted = false;

    // Completion Effect Components
    [Header("Completion Effect")]
    private Renderer cardRenderer;
    private Material originalMaterial;
    private bool isPartOfCompletedSet = false;
    private MMF_Player completionFeedbackPlayer;
    
    // Colors and effect settings
    private Color originalColor = Color.white;
    private Color completedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private Color highlightColor = new Color(1f, 1f, 0.8f, 1f); // Slight golden highlight

    void Awake()
    {
        col = GetComponent<Collider>();
        
        // Setup completion effect components
        SetupCompletionEffect();
    }

    private void SetupCompletionEffect()
    {
        cardRenderer = GetComponent<Renderer>();
        if (cardRenderer != null)
        {
            originalMaterial = cardRenderer.material;
            originalColor = cardRenderer.material.color;
        }

        // Try to get a second MMF_Player for completion effects
        MMF_Player[] players = GetComponents<MMF_Player>();
        if (players.Length > 1)
        {
            completionFeedbackPlayer = players[1];
        }
    }

    void Start()
    {
        // It will be face-up at the start
        transform.rotation = faceRotation;

        // Setup Feel animation system AFTER Start
        SetupFeelAnimation();

        // Wait for game to actually start before doing anything
        StartCoroutine(WaitForGameStart());
    }

    // FIXED: Setup Feel animation components properly
    private void SetupFeelAnimation()
    {
        // Get the MMF_Player component from this GameObject
        flipFeedbackPlayer = GetComponent<MMF_Player>();
        
        if (flipFeedbackPlayer == null)
        {
            Debug.LogError($"No MMF_Player found on {gameObject.name}! Please add MMF_Player component to your card prefabs.");
            isFeelSetupComplete = false;
            return;
        }

        // Initialize the feedback player properly
        flipFeedbackPlayer.Initialization();
        
        // Wait a frame then setup the rotation spring
        StartCoroutine(SetupRotationSpringDelayed());
    }

    // FIXED: Delayed setup to ensure proper initialization
    private IEnumerator SetupRotationSpringDelayed()
    {
        yield return null; // Wait one frame
        
        // Find the rotation spring feedback in the feedbacks list
        foreach (var feedback in flipFeedbackPlayer.FeedbacksList)
        {
            if (feedback is MMF_RotationSpring spring)
            {
                rotationSpring = spring;
                
                // FIXED: Configure spring properly
                rotationSpring.AnimateRotationTarget = this.transform;
                rotationSpring.Mode = MMF_RotationSpring.Modes.MoveTo;
                
                // FIXED: Use correct property names for spring settings
                rotationSpring.FrequencyX = 2f;
                rotationSpring.DampingX = 0.6f;
                
                isFeelSetupComplete = true;
                Debug.Log($"✅ Feel animation setup complete for {gameObject.name}");
                break;
            }
        }
        
        if (rotationSpring == null)
        {
            Debug.LogError($"❌ No MMF_RotationSpring found in {gameObject.name}! Please add Rotation Spring feedback to MMF_Player.");
            isFeelSetupComplete = false;
        }
    }

    // FIXED: Feel-based card flip method with proper error handling
    private void FlipCardWithFeel(bool toFaceUp)
    {
        if (!isFeelSetupComplete || rotationSpring == null)
        {
            Debug.LogWarning($"Feel not ready for {gameObject.name}, using fallback");
            StartCoroutine(RotateCard(toFaceUp ? faceRotation : backRotation, 0.5f));
            return;
        }
        
        // FIXED: Set target rotation properly
        Vector3 targetRotation = toFaceUp ? Vector3.zero : new Vector3(180f, 0f, 0f);
        
        // FIXED: Configure the spring rotation with proper values
        rotationSpring.MoveToRotationMin = targetRotation;
        rotationSpring.MoveToRotationMax = targetRotation;
        
        // Play the feedback
        flipFeedbackPlayer.PlayFeedbacks();
        
        Debug.Log($"🔄 Feel flip: {gameObject.name} to {(toFaceUp ? "face-up" : "face-down")}");
    }

    // Wait for GameManager to signal game start
    IEnumerator WaitForGameStart()
    {
        // Wait until game is started from GameManager
        while (GameManager.Instance == null || !GameManager.Instance.gameStarted)
        {
            yield return null;
        }
        
        // Now start the initial show and flip
        StartCoroutine(InitialShowAndFlip());
        
        // Trigger the countdown only once from one card
        if (!countdownStarted && GameManager.Instance != null)
        {
            countdownStarted = true;
            GameManager.Instance.StartInitialCountdown();
        }
    }

    // Initial seconds ke liye cards ko face-up dikhane ke liye
    IEnumerator InitialShowAndFlip()
    {
        // Card ko initially interaction se disable rakho
        col.enabled = false;
        isBusy = true;

        // Use the time from GameManager instead of hardcoding
        float viewTime = GameManager.Instance != null ? 
            GameManager.Instance.initialViewTime : 3f;
            
        // Wait for the view time
        yield return new WaitForSeconds(viewTime);

        // FIXED: Card ko face-down karo with Feel animation
        if (isFeelSetupComplete)
        {
            FlipCardWithFeel(false);
            yield return new WaitForSeconds(1.0f); // Increased wait time for spring animation
        }
        else
        {
            yield return RotateCard(backRotation, 0.5f);
        }

        // Card ke interaction ko enable karo
        col.enabled = true;
        isBusy = false;
    }

    public void OnClick()
    {
        // Don't allow interaction until game has started
        if (GameManager.Instance != null && !GameManager.Instance.gameStarted) return;

        if (isBusy || isMatched) return;

        // Increment attempts when a card is clicked
        if (GameManager.Instance != null && !isFlipped)
        {
            GameManager.Instance.IncrementAttempts();
        }

        // Start flip
        StartCoroutine(FlipRoutine());
    }
    void OnMouseDown()
    {
        // Don't allow interaction until game has started
        if (GameManager.Instance != null && !GameManager.Instance.gameStarted) return;
        
        if (isBusy) return;
        if (isMatched) return;
        
        // Track card interaction for analytics (timing tracking)
        GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
        if (analyticsObj != null)
        {
            analyticsObj.SendMessage("TrackCardInteraction", gameObject, SendMessageOptions.DontRequireReceiver);
        }
        
        // Increment attempts when a card is clicked
        if (GameManager.Instance != null && !isFlipped)
        {
            GameManager.Instance.IncrementAttempts();
        }
        
        StartCoroutine(FlipRoutine());
    }

    IEnumerator FlipRoutine()
    {
        isBusy = true;
        col.enabled = false;

        if (!isFlipped)
        {
            // Card ko face-up karo with Feel animation
            if (isFeelSetupComplete)
            {
                FlipCardWithFeel(true);
                yield return new WaitForSeconds(1.0f); // Increased wait time
            }
            else
            {
                yield return RotateCard(faceRotation, 0.5f);
            }
            
            isFlipped = true;

            // Check if this card matches with any player hand card
            bool hasMatch = CheckForPlayerHandMatch();

            if (hasMatch)
            {
                // CORRECT MATCH - Track analytics
                GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
                if (analyticsObj != null)
                {
                    var analyticsComponent = analyticsObj.GetComponent(typeof(MonoBehaviour));
                    if (analyticsComponent != null)
                    {
                        // Use SendMessage to avoid compilation issues
                        analyticsObj.SendMessage("TrackCorrectAttempt", gameObject, SendMessageOptions.DontRequireReceiver);
                    }
                }
                
                // Card match ho gaya, permanently face-up rakho
                isMatched = true;

                // Notify GameManager that a match has been found
                GameManager.Instance.CardMatched(cardTypeId, this);
            }
            else
            {
                // INCORRECT MATCH - Track analytics
                GameObject analyticsObj = GameObject.Find("MahjongAnalyticsManager");
                if (analyticsObj != null)
                {
                    var analyticsComponent = analyticsObj.GetComponent(typeof(MonoBehaviour));
                    if (analyticsComponent != null)
                    {
                        // Use SendMessage to avoid compilation issues
                        analyticsObj.SendMessage("TrackIncorrectAttempt", gameObject, SendMessageOptions.DontRequireReceiver);
                    }
                }
                
                // Play wrong match sound
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlayWrongMatchSound();
                }
                
                // No match, wait and flip back
                yield return new WaitForSeconds(2f);

                // Wapas face-down position mein jaao with Feel animation
                if (isFeelSetupComplete)
                {
                    FlipCardWithFeel(false);
                    yield return new WaitForSeconds(1.0f);
                }
                else
                {
                    yield return RotateCard(backRotation, 0.5f);
                }
                
                isFlipped = false;

                // Enable interaction again
                yield return new WaitForSeconds(0.5f);
                col.enabled = true;
                isBusy = false;
            }
        }
    }

    // Check if this card matches any player hand card
    bool CheckForPlayerHandMatch()
    {
        // Find Player Hand cards
        GameObject playerHandParent = GameObject.FindGameObjectWithTag("PlayerHand");
        if (playerHandParent == null) return false;

        // Get all child objects which are the player hand cards
        foreach (Transform cardTransform in playerHandParent.transform)
        {
            // Get the PlayerHandCardController to access card ID
            PlayerHandCardController handCard = cardTransform.GetComponent<PlayerHandCardController>();
            if (handCard != null && handCard.cardTypeId == this.cardTypeId)
            {
                // Match found!
                return true;
            }
        }

        return false;
    }

    // Keep existing rotation method as fallback
    IEnumerator RotateCard(Quaternion targetRot, float duration)
    {
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    // Method to set card type ID (called from CardSpawner)
    public void SetCardTypeId(int id)
    {
        cardTypeId = id;
    }

    // Add a new method to move the card to the collection grid
    public void MoveToCollectionGrid(Vector3 targetPosition)
    {
        // Start the card movement animation
        StartCoroutine(MoveCardToCollection(targetPosition));
    }

    // Coroutine to animate the card moving to the collection
    IEnumerator MoveCardToCollection(Vector3 targetPosition)
    {
        // Disable interactions during animation
        isBusy = true;
        col.enabled = false;
        
        // Store starting position
        Vector3 startPos = transform.position;
        
        // Animate card moving up then to target position
        float moveUpDuration = 0.5f;
        float moveOverDuration = 0.7f;
        float elapsed = 0f;
        
        // First move the card up
        while (elapsed < moveUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / moveUpDuration);
            Vector3 liftedPos = startPos + Vector3.up * 2f * t;
            transform.position = liftedPos;
            yield return null;
        }
        
        // Then move to the target position
        Vector3 liftedStartPos = transform.position;
        elapsed = 0f;
        
        while (elapsed < moveOverDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / moveOverDuration);
            transform.position = Vector3.Lerp(liftedStartPos, targetPosition + Vector3.up * 0.1f, t);
            yield return null;
        }
        
        // Ensure final position is exact
        transform.position = targetPosition + Vector3.up * 0.1f;
        
        // Keep the card in matched state
        isFlipped = true;
        isMatched = true;
        isBusy = false;
    }

    // FIXED: Clean destroy method
    void OnDestroy()
    {
        // Safely clean up Feel components
        if (flipFeedbackPlayer != null && flipFeedbackPlayer.IsPlaying)
        {
            try
            {
                flipFeedbackPlayer.StopFeedbacks();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error stopping feedbacks: {e.Message}");
            }
        }
        
        // Reset the static flag when any card is destroyed
        countdownStarted = false;
    }

    // FIXED: Debug method to test Feel animation
    [ContextMenu("Test Feel Animation")]
    private void TestFeelAnimation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test Feel Animation only works in Play mode!");
            return;
        }

        if (isFeelSetupComplete && rotationSpring != null)
        {
            FlipCardWithFeel(!isFlipped);
            Debug.Log("✅ Feel animation triggered!");
        }
        else
        {
            Debug.LogError("❌ Feel animation not set up properly! Check console for setup errors.");
        }
    }

    // Enhanced completion effect with multiple stages
    public void MarkAsCompletedSet()
    {
        if (isPartOfCompletedSet) return;
        
        isPartOfCompletedSet = true;
        StartCoroutine(EnhancedCompletionEffect());
    }

    IEnumerator EnhancedCompletionEffect()
    {
        if (cardRenderer == null) yield break;

        // Stage 1: Brief highlight flash
        yield return StartCoroutine(FlashHighlight());
        
        // Stage 2: Gentle scale pulse (if Feel is available)
        if (completionFeedbackPlayer != null)
        {
            completionFeedbackPlayer.PlayFeedbacks();
        }
        
        // Stage 3: Transition to completed color
        yield return StartCoroutine(TransitionToCompletedColor());
    }

    IEnumerator FlashHighlight()
    {
        // Quick flash to highlight color and back
        float flashDuration = 0.3f;
        Color startColor = cardRenderer.material.color;
        
        // Flash to highlight
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / flashDuration);
            cardRenderer.material.color = Color.Lerp(startColor, highlightColor, t);
            yield return null;
        }
        
        // Flash back to original
        elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / flashDuration);
            cardRenderer.material.color = Color.Lerp(highlightColor, startColor, t);
            yield return null;
        }
    }

    IEnumerator TransitionToCompletedColor()
    {
        // Slow transition to completion color
        float duration = 0.5f;
        float elapsed = 0f;
        Color startColor = cardRenderer.material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            cardRenderer.material.color = Color.Lerp(startColor, completedColor, t);
            yield return null;
        }

        cardRenderer.material.color = completedColor;
    }

    // Method to reset completion effect (if needed)
    public void ResetCompletionEffect()
    {
        if (cardRenderer != null)
        {
            cardRenderer.material.color = originalColor;
        }
        isPartOfCompletedSet = false;
    }
}