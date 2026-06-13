using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;
    
    [Header("Firebase Status")]
    public bool isFirebaseInitialized = false;
    
    [Header("Session Settings")]
    [SerializeField] private int sessionDurationDays = 7; // Control session duration in Unity Editor
    
    // Firebase references
    private FirebaseApp app;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    
    // Game selection tracking
    private string currentGameSelection = ""; // "GM1" for Mahjong, "GM2" for Taichi
    
    // Session management
    private const string SESSION_KEY = "UserSession";
    private const string SESSION_EXPIRY_KEY = "SessionExpiry";
    private const string USER_EMAIL_KEY = "UserEmail";
    
    // Events for UI updates
    public static event Action<bool> OnAuthenticationResult;
    public static event Action<string> OnAuthenticationError;
    public static event Action<bool> OnRegistrationResult;
    public static event Action<string> OnRegistrationError;
    public static event Action OnSessionExpired;
    
    // NEW: Additional events for password reset
    public static event Action<bool> OnPasswordResetEmailSent;
    public static event Action<string> OnPasswordResetError;
    public static event Action<bool, string> OnPasswordUpdateResult;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private async void InitializeFirebase()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                
                isFirebaseInitialized = true;
                Debug.Log("✅ Firebase initialized successfully");
                
                // Check existing session after Firebase initialization
                CheckExistingSession();
            }
            else
            {
                Debug.LogError($"❌ Firebase initialization failed: {dependencyStatus}");
                isFirebaseInitialized = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Firebase initialization error: {e.Message}");
            isFirebaseInitialized = false;
        }
    }
    
    // Check if there's a valid existing session
    private void CheckExistingSession()
    {
        if (HasValidSession())
        {
            string savedEmail = PlayerPrefs.GetString(USER_EMAIL_KEY, "");
            Debug.Log($"✅ Valid session found for user: {savedEmail}");
            
            // Notify UI that user is already logged in
            OnAuthenticationResult?.Invoke(true);
        }
        else
        {
            // Clear any expired session data
            ClearSessionData();
            Debug.Log("ℹ️ No valid session found, user needs to login");
        }
    }
    
    // Check if current session is valid
    public bool HasValidSession()
    {
        if (!PlayerPrefs.HasKey(SESSION_KEY) || !PlayerPrefs.HasKey(SESSION_EXPIRY_KEY))
        {
            return false;
        }
        
        string sessionExpiryString = PlayerPrefs.GetString(SESSION_EXPIRY_KEY);
        if (DateTime.TryParse(sessionExpiryString, out DateTime sessionExpiry))
        {
            return DateTime.Now < sessionExpiry;
        }
        
        return false;
    }
    
    // Create new session
    private void CreateSession(string userEmail)
    {
        DateTime expiryDate = DateTime.Now.AddDays(sessionDurationDays);
        
        PlayerPrefs.SetString(SESSION_KEY, "active");
        PlayerPrefs.SetString(SESSION_EXPIRY_KEY, expiryDate.ToString());
        PlayerPrefs.SetString(USER_EMAIL_KEY, userEmail);
        PlayerPrefs.Save();
        
        Debug.Log($"✅ Session created for {userEmail}, expires on: {expiryDate}");
    }
    
    // Clear session data
    private void ClearSessionData()
    {
        PlayerPrefs.DeleteKey(SESSION_KEY);
        PlayerPrefs.DeleteKey(SESSION_EXPIRY_KEY);
        PlayerPrefs.DeleteKey(USER_EMAIL_KEY);
        PlayerPrefs.Save();
        
        Debug.Log("🧹 Session data cleared");
    }
    
    // Get remaining session time
    public TimeSpan GetRemainingSessionTime()
    {
        if (HasValidSession())
        {
            string sessionExpiryString = PlayerPrefs.GetString(SESSION_EXPIRY_KEY);
            if (DateTime.TryParse(sessionExpiryString, out DateTime sessionExpiry))
            {
                TimeSpan remaining = sessionExpiry - DateTime.Now;
                return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
            }
        }
        return TimeSpan.Zero;
    }
    
    // Login user with email and password
    public async Task<bool> LoginUser(string email, string password)
    {
        if (!isFirebaseInitialized)
        {
            OnAuthenticationError?.Invoke("Firebase not initialized");
            return false;
        }
        
        try
        {
            var authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            var user = authResult.User;
            
            if (user != null)
            {
                Debug.Log($"✅ User logged in: {user.Email}");
                
                // Create session
                CreateSession(user.Email);
                
                // Store user data in Firestore
                await StoreUserLoginData(user.UserId, user.Email);
                
                OnAuthenticationResult?.Invoke(true);
                return true;
            }
            else
            {
                OnAuthenticationError?.Invoke("Login failed - Invalid credentials");
                return false;
            }
        }
        catch (FirebaseException e)
        {
            string errorMessage = GetFirebaseErrorMessage(e);
            OnAuthenticationError?.Invoke(errorMessage);
            Debug.LogError($"❌ Login error: {errorMessage}");
            return false;
        }
    }
    
    // Register new user
    public async Task<bool> RegisterUser(string email, string password)
    {
        if (!isFirebaseInitialized)
        {
            OnRegistrationError?.Invoke("Firebase not initialized");
            return false;
        }
        
        try
        {
            var authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var user = authResult.User;
            
            if (user != null)
            {
                Debug.Log($"✅ User registered: {user.Email}");
                
                // Create user profile in Firestore
                await CreateUserProfile(user.UserId, user.Email);
                
                // Sign out after registration (user needs to login)
                auth.SignOut();
                
                OnRegistrationResult?.Invoke(true);
                return true;
            }
            else
            {
                OnRegistrationError?.Invoke("Registration failed");
                return false;
            }
        }
        catch (FirebaseException e)
        {
            string errorMessage = GetFirebaseErrorMessage(e);
            OnRegistrationError?.Invoke(errorMessage);
            Debug.LogError($"❌ Registration error: {errorMessage}");
            return false;
        }
    }
    
    // Create user profile in Firestore
    private async Task CreateUserProfile(string userId, string email)
    {
        try
        {
            var userRef = db.Collection("users").Document(userId);
            var userData = new
            {
                email = email,
                createdAt = Timestamp.GetCurrentTimestamp(),
                lastLogin = Timestamp.GetCurrentTimestamp(),
                gamesPlayed = 0,
                highScore = 0
            };
            
            await userRef.SetAsync(userData);
            Debug.Log($"✅ User profile created for: {email}");
            
            // Create GM1 collection for Mahjong scores
            await CreateMahjongGameData(userId);
            
            // Create GM2 collection for Taichi scores
            await CreateTaichiGameData(userId);

            // 🔥 ADD THIS: Create GM3 collection for Food Choose scores
            await CreateFoodChooseGameData(userId);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error creating user profile: {e.Message}");
        }
    }
    
    // Create GM1 collection for Mahjong game data
    private async Task CreateMahjongGameData(string userId)
    {
        try
        {
            var gm1Ref = db.Collection("users").Document(userId).Collection("GM1").Document("scores");
            var mahjongData = new
            {
                // Last played scores (always updated)
                EasyScore = 0,
                MediumScore = 0,
                HardScore = 0,
                // New tracking fields
                TimeTaken = 0,
                FailedAttempts = 0,
                Wins = 0,
                Lose = 0,
                lastPlayed = Timestamp.GetCurrentTimestamp()
            };
            
            await gm1Ref.SetAsync(mahjongData);
            Debug.Log($"✅ GM1 (Mahjong) collection created for user: {userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error creating GM1 collection: {e.Message}");
        }
    }
    
    // Create GM2 collection for Taichi (PianoTile) game data - UPDATED TO NEW STRUCTURE
    private async Task CreateTaichiGameData(string userId)
    {
        try
        {
            var gm2Ref = db.Collection("users").Document(userId).Collection("GM2").Document("scores");
            var taichiData = new
            {
                // New GM2 structure
                LastScore = 0,                               // Last played game score
                PreviousScores = new List<int>(),            // Last 5 scores (most recent first)
                TopHighScores = new List<int>(),             // Top 10 high scores (desc)
                MaxDelayBetweenTriggers = 0.0,               // Max delay observed between two triggers across all sessions
                lastPlayed = Timestamp.GetCurrentTimestamp()
            };
            
            await gm2Ref.SetAsync(taichiData);
            Debug.Log($"✅ GM2 (Taichi) collection created for user: {userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error creating GM2 collection: {e.Message}");
        }
    }
    // ---------- GM3 (Food Choose) Methods ----------

    // 1. Initialize GM3 Database structure
    // 1. Initialize GM3 Database structure
    private async Task CreateFoodChooseGameData(string userId)
    {
        try
        {
            var gm3Ref = db.Collection("users").Document(userId).Collection("GM3").Document("scores");
            var foodData = new
            {
                EasyScore = 0,
                NormalScore = 0,
                HardScore = 0,
                LossByTime = 0, // Keep only Time loss
                Wins = 0,
                lastPlayed = Timestamp.GetCurrentTimestamp()
            };

            await gm3Ref.SetAsync(foodData);
            Debug.Log($"✅ GM3 (Food Choose) collection created for user: {userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error creating GM3 collection: {e.Message}");
        }
    }

    // 2. Game Selection Method (Call this when opening the Food Choose game menu)
    public void SelectFoodChooseGame()
    {
        currentGameSelection = "GM3";
        Debug.Log("🎮 Game selection set to GM3 (Food Choose)");
    }

    // 3. Save GM3 Match Results
    public async Task UpdateGM3Score(string difficulty, int score, string lossReason, bool isWin, List<string> chosenFoods)
    {
        if (!isFirebaseInitialized) return;

        var user = GetCurrentUser();
        if (user == null && !HasValidSession()) return;

        string userId = user != null ? user.UserId : GetCurrentUserEmail().Replace(".", "_").Replace("@", "_");
        var gm3Ref = db.Collection("users").Document(userId).Collection("GM3").Document("scores");

        // Match difficulty to database field
        string scoreField = "NormalScore"; // Default
        if (difficulty.ToLower() == "easy") scoreField = "EasyScore";
        else if (difficulty.ToLower() == "normal") scoreField = "NormalScore";
        else if (difficulty.ToLower() == "hard") scoreField = "HardScore";

        var updateData = new Dictionary<string, object>
        {
            [scoreField] = score, // Saves the last score for this difficulty
            ["lastPlayed"] = Timestamp.GetCurrentTimestamp()
        };

      
        // Track Win/Loss reasons using Firestore's Increment feature
        if (isWin)
        {
            updateData["Wins"] = FieldValue.Increment(1);
        }
        else
        {
            // Now we only care about Time loss
            if (lossReason == "Time") updateData["LossByTime"] = FieldValue.Increment(1);
        }

        try
        {
            // 1. Update the overall aggregated stats
            await gm3Ref.SetAsync(updateData, SetOptions.MergeAll);

            // 🔥 NEW 2. Create a specific session record for THIS exact match
            // This will save inside users/{userId}/GM3/scores/MatchHistory/{randomId}
            var sessionHistoryRef = gm3Ref.Collection("MatchHistory").Document();

            var sessionData = new Dictionary<string, object>
            {
                ["Difficulty"] = difficulty,
                ["IsWin"] = isWin,
                ["LossReason"] = isWin ? "None" : lossReason,
                ["FinalScore"] = score,
                ["ChosenFoods"] = chosenFoods, // Saves the array of food names!
                ["Timestamp"] = Timestamp.GetCurrentTimestamp()
            };

            await sessionHistoryRef.SetAsync(sessionData);

            Debug.Log($"✅ GM3 Score & Session Updated - Diff: {difficulty}, Score: {score}, Win: {isWin}");
            Debug.Log($"🍔 Foods chosen this match: {string.Join(", ", chosenFoods)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error updating GM3 score: {e.Message}");
        }
    }
    // Store user login data
    private async Task StoreUserLoginData(string userId, string email)
    {
        try
        {
            var userRef = db.Collection("users").Document(userId);
            var updateData = new
            {
                lastLogin = Timestamp.GetCurrentTimestamp()
            };
            
            await userRef.UpdateAsync("lastLogin", Timestamp.GetCurrentTimestamp());
            Debug.Log($"✅ Login data updated for: {email}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error updating login data: {e.Message}");
        }
    }
    
    // NEW: Set game selection to GM1 (Mahjong) - Call this on Mahjong button click
    public void SelectMahjongGame()
    {
        currentGameSelection = "GM1";
        Debug.Log("🎮 Game selection set to GM1 (Mahjong)");
    }
    
    // NEW: Set game selection to GM2 (Taichi) - Call this on Taichi button click
    public void SelectTaichiGame()
    {
        currentGameSelection = "GM2";
        Debug.Log("🎮 Game selection set to GM2 (Taichi)");
    }
    
    // NEW: Wrapper method for Unity OnClick events
    public void UpdateGamesPlayedCountWrapper()
    {
        _ = UpdateGamesPlayedCount(); // Fire and forget async call
    }
    
    // Keep the original async method as well
    public async Task UpdateGamesPlayedCount()
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return;
        }
        
        var user = GetCurrentUser();
        if (user == null)
        {
            Debug.LogError("❌ No user logged in");
            return;
        }
        
        try
        {
            var userRef = db.Collection("users").Document(user.UserId);
            
            // Get current games played count
            var userDoc = await userRef.GetSnapshotAsync();
            int currentGamesPlayed = 0;
            
            if (userDoc.Exists && userDoc.ContainsField("gamesPlayed"))
            {
                currentGamesPlayed = userDoc.GetValue<int>("gamesPlayed");
            }
            
            // Increment games played count
            await userRef.UpdateAsync("gamesPlayed", currentGamesPlayed + 1);
            
            Debug.Log($"✅ Games played count updated: {currentGamesPlayed + 1} for game: {currentGameSelection}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error updating games played count: {e.Message}");
        }
    }
    
    // Get current user
    public FirebaseUser GetCurrentUser()
    {
        return auth?.CurrentUser;
    }
    
    // Get current game selection
    public string GetCurrentGameSelection()
    {
        return currentGameSelection;
    }
    
    // Sign out user
    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            currentGameSelection = ""; // Reset game selection
            Debug.Log("✅ User signed out");
        }
    }
    
    // NEW: Logout function for Unity OnClick events
    public void LogoutUser()
    {
        try
        {
            string userEmail = "";
            
            if (auth != null && auth.CurrentUser != null)
            {
                userEmail = auth.CurrentUser.Email;
                // Sign out the user from Firebase
                auth.SignOut();
            }
            else if (HasValidSession())
            {
                userEmail = PlayerPrefs.GetString(USER_EMAIL_KEY, "Unknown");
            }
            
            // Clear session data
            ClearSessionData();
            
            // Reset game selection
            currentGameSelection = "";
            
            Debug.Log($"✅ User logged out: {userEmail}");
            
            // Optional: You can add an event here to notify UI components
            // OnLogoutComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error during logout: {e.Message}");
        }
    }
    
    // NEW: Check if user is currently logged in (either Firebase or session)
    public bool IsUserLoggedIn()
    {
        return (auth != null && auth.CurrentUser != null) || HasValidSession();
    }
    
    // NEW: Get current user email (useful for UI display)
    public string GetCurrentUserEmail()
    {
        if (auth?.CurrentUser != null)
        {
            return auth.CurrentUser.Email;
        }
        else if (HasValidSession())
        {
            return PlayerPrefs.GetString(USER_EMAIL_KEY, "");
        }
        return "";
    }
    
    // NEW: Get session info for debugging
    public string GetSessionInfo()
    {
        if (HasValidSession())
        {
            TimeSpan remaining = GetRemainingSessionTime();
            return $"Session active. Remaining: {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m";
        }
        return "No active session";
    }

    // ---------- GM2 (PianoTile) helpers ----------
    private static List<int> ToIntList(object firestoreArray)
    {
        var result = new List<int>();
        if (firestoreArray == null) return result;
        try
        {
            if (firestoreArray is IEnumerable<object> objs)
            {
                foreach (var o in objs)
                {
                    if (o is int i) result.Add(i);
                    else if (o is long l) result.Add(checked((int)l));
                    else if (o is double d) result.Add((int)Math.Round(d));
                    else if (o != null && int.TryParse(o.ToString(), out var parsed)) result.Add(parsed);
                }
            }
            else if (firestoreArray is IEnumerable<int> ints)
            {
                result.AddRange(ints);
            }
        }
        catch { /* ignore parse issues */ }
        return result;
    }

    // Update GM2 structure with new score and optional max delay observed this session
    private async Task UpdateGM2Score(string userId, int score, float maxDelayBetweenTriggers)
    {
        var docRef = db.Collection("users").Document(userId).Collection("GM2").Document("scores");
        var snapshot = await docRef.GetSnapshotAsync();

        int prevLast = 0;
        List<int> previousScores = new List<int>();
        List<int> topHighScores = new List<int>();
        double currentMaxDelay = 0.0;

        if (snapshot.Exists)
        {
            if (snapshot.ContainsField("LastScore"))
                prevLast = snapshot.GetValue<int>("LastScore");

            if (snapshot.ContainsField("PreviousScores"))
            {
                var raw = snapshot.GetValue<object>("PreviousScores");
                previousScores = ToIntList(raw);
            }

            if (snapshot.ContainsField("TopHighScores"))
            {
                var raw = snapshot.GetValue<object>("TopHighScores");
                topHighScores = ToIntList(raw);
            }

            if (snapshot.ContainsField("MaxDelayBetweenTriggers"))
            {
                // Firestore stores numbers as double
                currentMaxDelay = snapshot.GetValue<double>("MaxDelayBetweenTriggers");
            }
        }
        else
        {
            // initialize baseline if missing
            await CreateTaichiGameData(userId);
        }

        // Maintain PreviousScores as the previous five BEFORE the new LastScore
        if (prevLast != 0 || snapshot.Exists)
        {
            // push previous last to front
            if (prevLast != 0) previousScores.Insert(0, prevLast);
            // trim to 5
            if (previousScores.Count > 5) previousScores = previousScores.Take(5).ToList();
        }

        // Maintain Top 10 highs
        topHighScores.Add(score);
        topHighScores = topHighScores
            .OrderByDescending(x => x)
            .Take(10)
            .ToList();

        // Update max delay if provided
        double newMaxDelay = currentMaxDelay;
        if (maxDelayBetweenTriggers >= 0f)
        {
            newMaxDelay = Math.Max(currentMaxDelay, (double)maxDelayBetweenTriggers);
        }

        var update = new Dictionary<string, object>
        {
            ["LastScore"] = score,
            ["PreviousScores"] = previousScores,
            ["TopHighScores"] = topHighScores,
            ["MaxDelayBetweenTriggers"] = newMaxDelay,
            ["lastPlayed"] = Timestamp.GetCurrentTimestamp()
        };

        await docRef.SetAsync(update, SetOptions.MergeAll);
    }

    // Public overload for GM2 gameplay to call directly
    public async Task UpdateGM2Score(int score, float maxDelayBetweenTriggers = -1f)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return;
        }

        var user = GetCurrentUser();
        if (user == null && !HasValidSession())
        {
            Debug.LogError("❌ No user logged in");
            return;
        }

        string userId = user != null ? user.UserId : GetCurrentUserEmail().Replace(".", "_").Replace("@", "_");
        await UpdateGM2Score(userId, score, maxDelayBetweenTriggers);
    }

    // Read GM2 summary
    public async Task<GM2ScoreSummary> GetGM2ScoreSummary()
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return null;
        }

        var user = GetCurrentUser();
        if (user == null && !HasValidSession())
        {
            Debug.LogError("❌ No user logged in");
            return null;
        }

        string userId = user != null ? user.UserId : GetCurrentUserEmail().Replace(".", "_").Replace("@", "_");
        var docRef = db.Collection("users").Document(userId).Collection("GM2").Document("scores");
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return new GM2ScoreSummary();
        }

        var summary = new GM2ScoreSummary();
        if (snapshot.ContainsField("LastScore")) summary.LastScore = snapshot.GetValue<int>("LastScore");
        if (snapshot.ContainsField("PreviousScores")) summary.PreviousScores = ToIntList(snapshot.GetValue<object>("PreviousScores"));
        if (snapshot.ContainsField("TopHighScores")) summary.TopHighScores = ToIntList(snapshot.GetValue<object>("TopHighScores"));
        if (snapshot.ContainsField("MaxDelayBetweenTriggers")) summary.MaxDelayBetweenTriggers = (float)snapshot.GetValue<double>("MaxDelayBetweenTriggers");
        return summary;
    }

    // Convert Firebase errors to user-friendly messages
    private string GetFirebaseErrorMessage(FirebaseException e)
    {
        switch (e.ErrorCode)
        {
            case (int)AuthError.InvalidEmail:
                return "Invalid email address";
            case (int)AuthError.WrongPassword:
                return "Incorrect password";
            case (int)AuthError.UserNotFound:
                return "No account found with this email";
            case (int)AuthError.EmailAlreadyInUse:
                return "Email already registered";
            case (int)AuthError.WeakPassword:
                return "Password is too weak";
            case (int)AuthError.NetworkRequestFailed:
                return "Network error. Please check your connection";
            default:
                return $"Authentication error: {e.Message}";
        }
    }

    // NEW: Send password reset email
    public async Task SendPasswordResetEmail(string email)
    {
        if (!isFirebaseInitialized)
        {
            OnPasswordResetError?.Invoke("Firebase not initialized");
            return;
        }

        try
        {
            await auth.SendPasswordResetEmailAsync(email);
            Debug.Log($"✅ Password reset email sent to: {email}");
            OnPasswordResetEmailSent?.Invoke(true);
        }
        catch (FirebaseException e)
        {
            string errorMessage = GetFirebaseErrorMessage(e);
            OnPasswordResetError?.Invoke(errorMessage);
            Debug.LogError($"❌ Password reset error: {errorMessage}");
        }
    }

    // NEW: Check if current user's email is verified
    public async Task<bool> CheckEmailVerification()
    {
        if (auth?.CurrentUser != null)
        {
            await auth.CurrentUser.ReloadAsync();
            return auth.CurrentUser.IsEmailVerified;
        }
        return false;
    }

    // NEW: Update user password (for authenticated user)
    public async Task UpdateUserPassword(string newPassword)
    {
        if (!isFirebaseInitialized)
        {
            OnPasswordUpdateResult?.Invoke(false, "Firebase not initialized");
            return;
        }

        var user = auth?.CurrentUser;
        if (user == null)
        {
            OnPasswordUpdateResult?.Invoke(false, "No user logged in");
            return;
        }

        try
        {
            await user.UpdatePasswordAsync(newPassword);
            Debug.Log("✅ Password updated successfully");
            
            // Sign out user after password update
            auth.SignOut();
            ClearSessionData();
            
            OnPasswordUpdateResult?.Invoke(true, "Password updated successfully");
        }
        catch (FirebaseException e)
        {
            string errorMessage = GetFirebaseErrorMessage(e);
            OnPasswordUpdateResult?.Invoke(false, errorMessage);
            Debug.LogError($"❌ Password update error: {errorMessage}");
        }
    }

    // NEW: Update game score and statistics based on difficulty and game result
    public async Task UpdateGameScore(string difficulty, int lastScore, int failedAttempts, float timeTaken, bool isWin)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return;
        }
        
        var user = GetCurrentUser();
        if (user == null && !HasValidSession())
        {
            Debug.LogError("❌ No user logged in");
            return;
        }

        try
        {
            // Get user ID (from Firebase user or session)
            string userId;
            if (user != null)
            {
                userId = user.UserId;
            }
            else
            {
                // If we're using session, we need to get user ID differently
                // For now, we'll use the email as identifier
                string userEmail = GetCurrentUserEmail();
                userId = userEmail.Replace(".", "_").Replace("@", "_"); // Simple conversion
            }

            // Determine which collection to update based on current game selection
            string gameCollection = string.IsNullOrEmpty(currentGameSelection) ? "GM1" : currentGameSelection;
            
            var gameRef = db.Collection("users").Document(userId).Collection(gameCollection).Document("scores");

            // If GM2 (PianoTile), route to the new structure updater and return
            if (gameCollection == "GM2")
            {
                // We only have lastScore here; if max delay is tracked elsewhere, call UpdateGM2Score with that value from there
                await UpdateGM2Score(userId, lastScore, -1f);
                Debug.Log($"✅ GM2 score updated (LastScore and lists). LastScore: {lastScore}");
                return;
            }
            
            // Determine which score field to update based on difficulty
            string scoreField = "";
            switch (difficulty.ToLower())
            {
                case "easy":
                    scoreField = "EasyScore";
                    break;
                case "medium":
                    scoreField = "MediumScore";
                    break;
                case "hard":
                    scoreField = "HardScore";
                    break;
                default:
                    Debug.LogError($"❌ Invalid difficulty: {difficulty}");
                    return;
            }

            // Get current statistics to increment
            var scoreDoc = await gameRef.GetSnapshotAsync();
            
            // Current values (with defaults if document doesn't exist)
            int currentWins = 0;
            int currentLoses = 0;
            int currentFailedAttempts = 0;
            
            if (scoreDoc.Exists)
            {
                if (scoreDoc.ContainsField("Wins"))
                    currentWins = scoreDoc.GetValue<int>("Wins");
                if (scoreDoc.ContainsField("Lose"))
                    currentLoses = scoreDoc.GetValue<int>("Lose");
                if (scoreDoc.ContainsField("FailedAttempts"))
                    currentFailedAttempts = scoreDoc.GetValue<int>("FailedAttempts");
            }

            // Calculate new statistics
            int newWins = currentWins + (isWin ? 1 : 0);
            int newLoses = currentLoses + (isWin ? 0 : 1);
            int newFailedAttempts = failedAttempts; // Store only last game's failed attempts (wrong selections)
            
            // Prepare update data - ALWAYS UPDATE (no comparison for "best" score)
            var updateData = new Dictionary<string, object>
            {
                [scoreField] = lastScore,                           // Last played score for this difficulty
                ["TimeTaken"] = Mathf.RoundToInt(timeTaken),       // Time taken in seconds (rounded)
                ["FailedAttempts"] = newFailedAttempts,            // Last game's failed attempts only
                ["Wins"] = newWins,                                // Total wins
                ["Lose"] = newLoses,                               // Total losses
                ["lastPlayed"] = Timestamp.GetCurrentTimestamp()   // Update last played timestamp
            };

            // Update the document with all new data
            await gameRef.UpdateAsync(updateData);
            
            string resultText = isWin ? "WIN" : "LOSE";
            Debug.Log($"✅ Game statistics updated for {difficulty} ({resultText}):");
            Debug.Log($"   Last Score: {lastScore}");
            Debug.Log($"   Time Taken: {timeTaken:F1}s");
            Debug.Log($"   Total Wins: {newWins}");
            Debug.Log($"   Total Losses: {newLoses}");
            Debug.Log($"   Last Game Failed Attempts (Wrong Selections): {newFailedAttempts}");
            Debug.Log($"   Game Collection: {gameCollection}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error updating game statistics: {e.Message}");
            throw; // Re-throw so the caller can handle it
        }
    }

    // LEGACY METHOD - Keep for backward compatibility but redirect to new method
    public async Task UpdateGameScore(string difficulty, int score)
    {
        // Default behavior: assume it's a loss with 60 seconds time if using old method
        // Since we don't have failed attempts info, assume score represents failed attempts
        await UpdateGameScore(difficulty, score, score, 60f, false);
    }

    // NEW: Get game statistics for display purposes
    public async Task<GameStatistics> GetGameStatistics(string gameType = "")
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return null;
        }
        
        var user = GetCurrentUser();
        if (user == null && !HasValidSession())
        {
            Debug.LogError("❌ No user logged in");
            return null;
        }

        try
        {
            // Get user ID
            string userId;
            if (user != null)
            {
                userId = user.UserId;
            }
            else
            {
                string userEmail = GetCurrentUserEmail();
                userId = userEmail.Replace(".", "_").Replace("@", "_");
            }

            // Use provided game type or current selection
            string gameCollection = string.IsNullOrEmpty(gameType) ? 
                                  (string.IsNullOrEmpty(currentGameSelection) ? "GM1" : currentGameSelection) : 
                                  gameType;
            
            var gameRef = db.Collection("users").Document(userId).Collection(gameCollection).Document("scores");
            // Special handling for GM2 new structure
            if (gameCollection == "GM2")
            {
                var gm2 = await GetGM2ScoreSummary();
                if (gm2 != null)
                {
                    // Map minimal fields for compatibility
                    return new GameStatistics
                    {
                        EasyScore = gm2.LastScore, // using LastScore as a general score
                        MediumScore = 0,
                        HardScore = 0,
                        TimeTaken = 0,
                        FailedAttempts = 0,
                        Wins = 0,
                        Loses = 0,
                        GameType = gameCollection
                    };
                }
                return new GameStatistics { GameType = gameCollection };
            }

            var scoreDoc = await gameRef.GetSnapshotAsync();
            
            if (scoreDoc.Exists)
            {
                var stats = new GameStatistics
                {
                    EasyScore = scoreDoc.ContainsField("EasyScore") ? scoreDoc.GetValue<int>("EasyScore") : 0,
                    MediumScore = scoreDoc.ContainsField("MediumScore") ? scoreDoc.GetValue<int>("MediumScore") : 0,
                    HardScore = scoreDoc.ContainsField("HardScore") ? scoreDoc.GetValue<int>("HardScore") : 0,
                    TimeTaken = scoreDoc.ContainsField("TimeTaken") ? scoreDoc.GetValue<int>("TimeTaken") : 0,
                    FailedAttempts = scoreDoc.ContainsField("FailedAttempts") ? scoreDoc.GetValue<int>("FailedAttempts") : 0,
                    Wins = scoreDoc.ContainsField("Wins") ? scoreDoc.GetValue<int>("Wins") : 0,
                    Loses = scoreDoc.ContainsField("Lose") ? scoreDoc.GetValue<int>("Lose") : 0,
                    GameType = gameCollection
                };
                
                Debug.Log($"📊 Retrieved game statistics for {gameCollection}:");
                Debug.Log($"   Easy: {stats.EasyScore}, Medium: {stats.MediumScore}, Hard: {stats.HardScore}");
                Debug.Log($"   Wins: {stats.Wins}, Losses: {stats.Loses}, Failed Attempts: {stats.FailedAttempts}");
                
                return stats;
            }
            else
            {
                Debug.LogWarning($"⚠️ No statistics found for {gameCollection}");
                return new GameStatistics { GameType = gameCollection };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error retrieving game statistics: {e.Message}");
            return null;
        }
    }
    
    // NEW: Save hand data session to Firebase
    public async Task SaveHandDataSession(HandDataSession session)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return;
        }
        
        var user = GetCurrentUser();
        if (user == null && !HasValidSession())
        {
            Debug.LogError("❌ No user logged in");
            return;
        }
        
        try
        {
            string userId = user != null ? user.UserId : GetCurrentUserEmail().Replace(".", "_").Replace("@", "_");
            
            // Create a unique document ID using timestamp
            string docId = $"{session.GameType}_{session.SessionStartTime:yyyyMMdd_HHmmss}";
            
            var handDataRef = db.Collection("users")
                .Document(userId)
                .Collection("GameSessionsData")
                .Document(docId);
            
            // Convert session to dictionary for Firestore
            var sessionData = new Dictionary<string, object>
            {
                // Basic session info
                ["gameType"] = session.GameType,
                ["sessionStartTime"] = Timestamp.FromDateTime(session.SessionStartTime.ToUniversalTime()),
                ["sessionEndTime"] = Timestamp.FromDateTime(session.SessionEndTime.ToUniversalTime()),
                ["sessionDuration"] = session.SessionDuration,
                ["userId"] = userId,
                
                // Game metadata
                ["gameDifficulty"] = session.GameDifficulty ?? "",
                ["gameCompleted"] = session.GameCompleted,
                ["finalScore"] = session.FinalScore,
                
                // Common game statistics
                ["totalAttempts"] = session.TotalAttempts,
                ["failedAttempts"] = session.FailedAttempts,
                ["timeTaken"] = session.TimeTaken,
                ["accuracy"] = session.Accuracy,
                
                // Sample counts
                ["leftHandSampleCount"] = session.LeftHandSamples?.Count ?? 0,
                ["rightHandSampleCount"] = session.RightHandSamples?.Count ?? 0
            };
            
            // Add Mahjong-specific fields
            if (session.GameType == "Mahjong")
            {
                sessionData["timeLimit"] = session.TimeLimit;
                sessionData["matchesCompleted"] = session.MatchesCompleted;
                sessionData["totalMatchesNeeded"] = session.TotalMatchesNeeded;
            }
            
            // Add Taichi-specific fields
            if (session.GameType == "Taichi")
            {
                sessionData["maxCombo"] = session.MaxCombo;
                sessionData["maxDelayBetweenActions"] = session.MaxDelayBetweenActions;
                sessionData["totalCubesSpawned"] = session.TotalCubesSpawned;
            }

            // Add Food Choose-specific fields
            if (session.GameType == "FoodChoose")
            {
                sessionData["finalOil"] = session.FinalOil;
                sessionData["finalSugar"] = session.FinalSugar;
                sessionData["totalItemsPlaced"] = session.TotalItemsPlaced;
                sessionData["lossReason"] = session.LossReason ?? "None";
            }

            // Add summary statistics (lightweight)
            if (session.LeftHandStats != null)
            {
                sessionData["leftHandStats"] = new Dictionary<string, object>
                {
                    ["sampleCount"] = session.LeftHandStats.SampleCount,
                    ["averageAcceleration"] = new Dictionary<string, object>
                    {
                        ["x"] = session.LeftHandStats.AverageAcceleration.x,
                        ["y"] = session.LeftHandStats.AverageAcceleration.y,
                        ["z"] = session.LeftHandStats.AverageAcceleration.z
                    },
                    ["maxAcceleration"] = session.LeftHandStats.MaxAcceleration,
                    ["totalMovementDistance"] = session.LeftHandStats.TotalMovementDistance
                };
            }
            
            if (session.RightHandStats != null)
            {
                sessionData["rightHandStats"] = new Dictionary<string, object>
                {
                    ["sampleCount"] = session.RightHandStats.SampleCount,
                    ["averageAcceleration"] = new Dictionary<string, object>
                    {
                        ["x"] = session.RightHandStats.AverageAcceleration.x,
                        ["y"] = session.RightHandStats.AverageAcceleration.y,
                        ["z"] = session.RightHandStats.AverageAcceleration.z
                    },
                    ["maxAcceleration"] = session.RightHandStats.MaxAcceleration,
                    ["totalMovementDistance"] = session.RightHandStats.TotalMovementDistance
                };
            }
            
            // Store only a subset of samples to avoid document size limit (max ~50 samples per hand)
            int maxSamplesPerHand = 50;
            
            // Convert hand samples to serializable format (downsampled)
            if (session.LeftHandSamples != null && session.LeftHandSamples.Count > 0)
            {
                sessionData["leftHandSamples"] = ConvertHandSamplesToFirestore(session.LeftHandSamples, maxSamplesPerHand);
            }
            
            if (session.RightHandSamples != null && session.RightHandSamples.Count > 0)
            {
                sessionData["rightHandSamples"] = ConvertHandSamplesToFirestore(session.RightHandSamples, maxSamplesPerHand);
            }
            
            await handDataRef.SetAsync(sessionData);
            
            Debug.Log($"✅ Game session data saved to Firebase: {docId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error saving game session data: {e.Message}");
        }
    }
    
    // Helper method to convert hand samples to Firestore-compatible format with downsampling
    private List<Dictionary<string, object>> ConvertHandSamplesToFirestore(List<HandDataSample> samples, int maxSamples = 50)
    {
        var result = new List<Dictionary<string, object>>();
        
        if (samples == null || samples.Count == 0)
            return result;
        
        // Downsample if we have too many samples
        int step = Mathf.Max(1, samples.Count / maxSamples);
        
        for (int i = 0; i < samples.Count; i += step)
        {
            var sample = samples[i];
            var sampleDict = new Dictionary<string, object>
            {
                ["timestamp"] = sample.Timestamp,
                ["accelerometer"] = new Dictionary<string, object>
                {
                    ["x"] = sample.Accelerometer.x,
                    ["y"] = sample.Accelerometer.y,
                    ["z"] = sample.Accelerometer.z
                },
                ["orientation"] = new Dictionary<string, object>
                {
                    ["x"] = sample.Orientation.x,
                    ["y"] = sample.Orientation.y,
                    ["z"] = sample.Orientation.z,
                    ["w"] = sample.Orientation.w
                },
                ["trackerOffset"] = new Dictionary<string, object>
                {
                    ["x"] = sample.TrackerOffset.x,
                    ["y"] = sample.TrackerOffset.y,
                    ["z"] = sample.TrackerOffset.z
                },
                ["trackerSource"] = sample.TrackerSource,
                ["trackerLocation"] = sample.TrackerLocation,
                ["buttonPassthroughEnabled"] = sample.ButtonPassthroughEnabled
            };
            
            // Add joint data if available
            if (sample.Joints != null && sample.Joints.Count > 0)
            {
                var jointsArray = new List<Dictionary<string, object>>();
                foreach (var joint in sample.Joints)
                {
                    jointsArray.Add(new Dictionary<string, object>
                    {
                        ["name"] = joint.Name ?? "",
                        ["position"] = new Dictionary<string, object>
                        {
                            ["x"] = joint.Position.x,
                            ["y"] = joint.Position.y,
                            ["z"] = joint.Position.z
                        },
                        ["rotation"] = new Dictionary<string, object>
                        {
                            ["x"] = joint.Rotation.x,
                            ["y"] = joint.Rotation.y,
                            ["z"] = joint.Rotation.z,
                            ["w"] = joint.Rotation.w
                        }
                    });
                }
                sampleDict["joints"] = jointsArray;
            }
            
            result.Add(sampleDict);
        }
        
        return result;
    }
    
    // NEW: Retrieve hand data sessions for analysis
    public async Task<List<HandDataSession>> GetHandDataSessions(string gameType = "", int limit = 10)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("❌ Firebase not initialized");
            return null;
        }
        
        var user = GetCurrentUser();
        if (user == null && !HasValidSession())
        {
            Debug.LogError("❌ No user logged in");
            return null;
        }
        
        try
        {
            string userId = user != null ? user.UserId : GetCurrentUserEmail().Replace(".", "_").Replace("@", "_");
            
            var query = db.Collection("users")
                .Document(userId)
                .Collection("GameSessionsData")
                .OrderByDescending("sessionStartTime")
                .Limit(limit);
            
            var snapshot = await query.GetSnapshotAsync();
            var sessions = new List<HandDataSession>();
            
            foreach (var doc in snapshot.Documents)
            {
                if (!doc.Exists) continue;
                
                var sessionGameType = doc.GetValue<string>("gameType");
                
                // Filter by game type if specified
                if (!string.IsNullOrEmpty(gameType) && sessionGameType != gameType)
                    continue;
                
                var session = new HandDataSession
                {
                    GameType = sessionGameType,
                    SessionDuration = doc.GetValue<float>("sessionDuration"),
                    GameDifficulty = doc.ContainsField("gameDifficulty") ? doc.GetValue<string>("gameDifficulty") : "",
                    GameCompleted = doc.ContainsField("gameCompleted") && doc.GetValue<bool>("gameCompleted"),
                    FinalScore = doc.ContainsField("finalScore") ? doc.GetValue<int>("finalScore") : 0
                };
                
                if (doc.ContainsField("sessionStartTime"))
                {
                    var timestamp = doc.GetValue<Timestamp>("sessionStartTime");
                    session.SessionStartTime = timestamp.ToDateTime().ToLocalTime();
                }
                
                if (doc.ContainsField("sessionEndTime"))
                {
                    var timestamp = doc.GetValue<Timestamp>("sessionEndTime");
                    session.SessionEndTime = timestamp.ToDateTime().ToLocalTime();
                }
                
                sessions.Add(session);
            }
            
            Debug.Log($"✅ Retrieved {sessions.Count} game session data from Firebase");
            return sessions;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error retrieving game session data: {e.Message}");
            return null;
        }
    }
    
    // NEW: Update Mahjong analytics data in Firebase
    public async Task UpdateMahjongAnalytics(MahjongGameAnalytics analytics)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("Firebase not initialized");
            return;
        }
        
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("User not authenticated");
            return;
        }
        
        try
        {
            // Ensure GM1 is selected
            if (currentGameSelection != "GM1")
            {
                SelectMahjongGame();
            }
            
            var analyticsRef = db.Collection("users").Document(user.UserId).Collection("GM1").Document("analytics");
            
            var analyticsData = new Dictionary<string, object>
            {
                ["TotalNoofAttempts"] = analytics.TotalNoofAttempts,
                ["MaxNoofIncorrectAttempts"] = analytics.MaxNoofIncorrectAttempts,
                ["MaxDelayBetweenAttempts"] = analytics.MaxDelayBetweenAttempts,
                ["RememberedCardsSelectedOnStart"] = analytics.RememberedCardsSelectedOnStart,
                
                // NEW: Set completion timing fields
                ["TimeTakenForSetFirst"] = analytics.TimeTakenForSetFirst,
                ["TimeTakenForSetSecond"] = analytics.TimeTakenForSetSecond,
                ["TimeTakenForSetThird"] = analytics.TimeTakenForSetThird,
                
                ["GameDifficulty"] = analytics.GameDifficulty,
                ["GameCompleted"] = analytics.GameCompleted,
                ["GameEndTime"] = Timestamp.FromDateTime(analytics.GameEndTime.ToUniversalTime()),
                ["lastUpdated"] = Timestamp.GetCurrentTimestamp()
            };
            
            await analyticsRef.SetAsync(analyticsData);
            
            Debug.Log($"✅ Mahjong analytics updated successfully:");
            Debug.Log($"   Total Attempts: {analytics.TotalNoofAttempts}");
            Debug.Log($"   Max Incorrect on Tile: {analytics.MaxNoofIncorrectAttempts}");
            Debug.Log($"   Max Hesitation Time: {analytics.MaxDelayBetweenAttempts:F2}s");
            Debug.Log($"   Remembered from Start: {analytics.RememberedCardsSelectedOnStart}");
            Debug.Log($"   Set 1 Time: {analytics.TimeTakenForSetFirst:F2}s");
            Debug.Log($"   Set 2 Time: {analytics.TimeTakenForSetSecond:F2}s");
            Debug.Log($"   Set 3 Time: {analytics.TimeTakenForSetThird:F2}s");
            Debug.Log($"   Difficulty: {analytics.GameDifficulty}");
            Debug.Log($"   Game Completed: {analytics.GameCompleted}");
            
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error updating Mahjong analytics: {e.Message}");
        }
    }
    
    // NEW: Retrieve Mahjong analytics data from Firebase
    public async Task<MahjongGameAnalytics> GetMahjongAnalytics()
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("Firebase not initialized");
            return null;
        }
        
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("User not authenticated");
            return null;
        }
        
        try
        {
            var analyticsRef = db.Collection("users").Document(user.UserId).Collection("GM1").Document("analytics");
            var analyticsDoc = await analyticsRef.GetSnapshotAsync();
            
            if (analyticsDoc.Exists)
            {
                var analytics = new MahjongGameAnalytics
                {
                    TotalNoofAttempts = analyticsDoc.ContainsField("TotalNoofAttempts") ? analyticsDoc.GetValue<int>("TotalNoofAttempts") : 0,
                    MaxNoofIncorrectAttempts = analyticsDoc.ContainsField("MaxNoofIncorrectAttempts") ? analyticsDoc.GetValue<int>("MaxNoofIncorrectAttempts") : 0,
                    MaxDelayBetweenAttempts = analyticsDoc.ContainsField("MaxDelayBetweenAttempts") ? (float)analyticsDoc.GetValue<double>("MaxDelayBetweenAttempts") : 0f,
                    RememberedCardsSelectedOnStart = analyticsDoc.ContainsField("RememberedCardsSelectedOnStart") ? analyticsDoc.GetValue<int>("RememberedCardsSelectedOnStart") : 0,
                    
                    // NEW: Set completion timing fields
                    TimeTakenForSetFirst = analyticsDoc.ContainsField("TimeTakenForSetFirst") ? (float)analyticsDoc.GetValue<double>("TimeTakenForSetFirst") : 0f,
                    TimeTakenForSetSecond = analyticsDoc.ContainsField("TimeTakenForSetSecond") ? (float)analyticsDoc.GetValue<double>("TimeTakenForSetSecond") : 0f,
                    TimeTakenForSetThird = analyticsDoc.ContainsField("TimeTakenForSetThird") ? (float)analyticsDoc.GetValue<double>("TimeTakenForSetThird") : 0f,
                    
                    GameDifficulty = analyticsDoc.ContainsField("GameDifficulty") ? analyticsDoc.GetValue<string>("GameDifficulty") : "",
                    GameCompleted = analyticsDoc.ContainsField("GameCompleted") ? analyticsDoc.GetValue<bool>("GameCompleted") : false
                };
                
                if (analyticsDoc.ContainsField("GameEndTime"))
                {
                    var timestamp = analyticsDoc.GetValue<Timestamp>("GameEndTime");
                    analytics.GameEndTime = timestamp.ToDateTime();
                }
                
                Debug.Log($"📊 Retrieved Mahjong analytics:");
                Debug.Log($"   Total Attempts: {analytics.TotalNoofAttempts}");
                Debug.Log($"   Max Incorrect on Tile: {analytics.MaxNoofIncorrectAttempts}");
                Debug.Log($"   Max Hesitation Time: {analytics.MaxDelayBetweenAttempts:F2}s");
                Debug.Log($"   Remembered from Start: {analytics.RememberedCardsSelectedOnStart}");
                Debug.Log($"   Set 1 Time: {analytics.TimeTakenForSetFirst:F2}s");
                Debug.Log($"   Set 2 Time: {analytics.TimeTakenForSetSecond:F2}s");
                Debug.Log($"   Set 3 Time: {analytics.TimeTakenForSetThird:F2}s");
                
                return analytics;
            }
            else
            {
                Debug.LogWarning("⚠️ No Mahjong analytics found");
                return new MahjongGameAnalytics();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error retrieving Mahjong analytics: {e.Message}");
            return null;
        }
    }
}

// NEW: Data structure for Mahjong game analytics
[System.Serializable]
public class MahjongGameAnalytics
{
    public int TotalNoofAttempts = 0;
    public int MaxNoofIncorrectAttempts = 0;
    public float MaxDelayBetweenAttempts = 0f; // HesitationTime in seconds
    public int RememberedCardsSelectedOnStart = 0;
    
    // NEW: Set completion timing fields
    public float TimeTakenForSetFirst = 0f;   // Time to complete first set (in seconds)
    public float TimeTakenForSetSecond = 0f;  // Time to complete second set (in seconds) 
    public float TimeTakenForSetThird = 0f;   // Time to complete third set (in seconds)
    
    public string GameDifficulty = "";
    public DateTime GameEndTime;
    public bool GameCompleted = false;
    
    public MahjongGameAnalytics()
    {
        TotalNoofAttempts = 0;
        MaxNoofIncorrectAttempts = 0;
        MaxDelayBetweenAttempts = 0f;
        RememberedCardsSelectedOnStart = 0;
        TimeTakenForSetFirst = 0f;
        TimeTakenForSetSecond = 0f;
        TimeTakenForSetThird = 0f;
        GameDifficulty = "";
        GameEndTime = DateTime.Now;
        GameCompleted = false;
    }
}

// NEW: Data structure for game statistics
[System.Serializable]
public class GameStatistics
{
    public int EasyScore;
    public int MediumScore;  
    public int HardScore;
    public int TimeTaken;
    public int FailedAttempts;
    public int Wins;
    public int Loses;
    public string GameType;
    
    public int TotalGames => Wins + Loses;
    public float WinRate => TotalGames > 0 ? (float)Wins / TotalGames * 100f : 0f;
}

// NEW: Data structure for GM2 (PianoTile) score summary
[System.Serializable]
public class GM2ScoreSummary
{
    public int LastScore;
    public List<int> PreviousScores = new List<int>(); // most recent first, max 5
    public List<int> TopHighScores = new List<int>();  // desc, max 10
    public float MaxDelayBetweenTriggers;              // overall max observed
}