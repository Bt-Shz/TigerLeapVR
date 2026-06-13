using System;
using System.Threading.Tasks;
using UnityEngine;

public class BackendFacade : MonoBehaviour
{
    public static BackendFacade Instance { get; private set; }

    public static event Action<bool> OnAuthenticationResult;
    public static event Action<string> OnAuthenticationError;
    public static event Action<bool> OnRegistrationResult;
    public static event Action<string> OnRegistrationError;
    public static event Action OnSessionExpired;
    public static event Action<string> OnUploadFailed;

    private WatchSdkApiClient _apiClient;
    private AuthService _authService;
    private PerformanceService _performanceService;

    public bool IsConfigured => WatchSdkConfig.IsConfigured;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _apiClient = new WatchSdkApiClient();
        _authService = new AuthService(_apiClient, ownsClient: false);
        _performanceService = new PerformanceService(_apiClient, ownsClient: false);
        _authService.OnSessionExpired += HandleSessionExpired;

        RestoreSessionOnColdStart();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (_authService != null)
        {
            _authService.OnSessionExpired -= HandleSessionExpired;
            _authService.Dispose();
        }

        _performanceService?.Dispose();
        _apiClient?.Dispose();
    }

    private void RestoreSessionOnColdStart()
    {
        _authService.RestoreSessionOnColdStart();

        if (IsUserLoggedIn())
        {
            Debug.Log($"Valid session restored for {GetCurrentUserEmail()}");
            OnAuthenticationResult?.Invoke(true);
        }
    }

    private void HandleSessionExpired()
    {
        OnSessionExpired?.Invoke();
    }

    public bool IsUserLoggedIn() => WatchSdkSessionStore.IsSignedIn();

    public string GetCurrentUserEmail() => WatchSdkSessionStore.GetUserEmail();

    public string GetSessionInfo()
    {
        if (!IsUserLoggedIn())
        {
            return "No active session";
        }

        var refreshExpires = WatchSdkSessionStore.GetRefreshTokenExpiresOn();
        if (refreshExpires.HasValue)
        {
            var remaining = refreshExpires.Value.ToUniversalTime() - DateTime.UtcNow;
            if (remaining.TotalSeconds > 0)
            {
                return $"Session active. Refresh expires in {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m";
            }
        }

        return "Session active";
    }

    public async Task LoginUser(string email, string password)
    {
        try
        {
            await _authService.LoginAsync(email, password);
            OnAuthenticationResult?.Invoke(true);
        }
        catch (WatchSdkApiException ex)
        {
            if (ex.StatusCode == 401)
            {
                HandleSessionExpired();
            }

            OnAuthenticationError?.Invoke(ex.Message);
        }
        catch (Exception ex)
        {
            OnAuthenticationError?.Invoke(ex.Message);
        }
    }

    public async Task RegisterUser(string email, string password, string displayName = null)
    {
        try
        {
            await _authService.RegisterAsync(email, password, displayName);
            OnRegistrationResult?.Invoke(true);
            OnAuthenticationResult?.Invoke(true);
        }
        catch (WatchSdkApiException ex)
        {
            OnRegistrationError?.Invoke(ex.Message);
        }
        catch (Exception ex)
        {
            OnRegistrationError?.Invoke(ex.Message);
        }
    }

    public async void LogoutUser()
    {
        await _authService.LogoutAsync();
    }

    public async void UploadMahjongSession(int attempts, int failedAttempts, float timeTakenSeconds)
    {
        if (!IsUserLoggedIn())
        {
            RaiseUploadFailed("Sign in to upload game results.");
            return;
        }

        try
        {
            await _performanceService.UploadMahjongSessionAsync(
                attempts,
                failedAttempts,
                timeTakenSeconds);
            Debug.Log("Mahjong performance uploaded.");
        }
        catch (WatchSdkApiException ex)
        {
            if (ex.StatusCode == 401)
            {
                HandleSessionExpired();
            }

            RaiseUploadFailed(ex.Message);
        }
        catch (Exception ex)
        {
            RaiseUploadFailed(ex.Message);
        }
    }

    public async void UploadEasyHandSession(int cubesCaught, int currentMisses, float sessionSeconds)
    {
        if (!IsUserLoggedIn())
        {
            RaiseUploadFailed("Sign in to upload game results.");
            return;
        }

        try
        {
            await _performanceService.UploadEasyHandSessionAsync(
                cubesCaught,
                currentMisses,
                sessionSeconds);
            Debug.Log("EasyHand performance uploaded.");
        }
        catch (WatchSdkApiException ex)
        {
            if (ex.StatusCode == 401)
            {
                HandleSessionExpired();
            }

            RaiseUploadFailed(ex.Message);
        }
        catch (Exception ex)
        {
            RaiseUploadFailed(ex.Message);
        }
    }

    private static void RaiseUploadFailed(string message)
    {
        Debug.LogWarning($"Upload failed: {message}");
        OnUploadFailed?.Invoke(message);
    }
}
