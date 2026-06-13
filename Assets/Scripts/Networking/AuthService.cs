using System;
using System.Threading.Tasks;

public sealed class AuthService : IDisposable
{
    public event Action OnSessionExpired;

    private readonly WatchSdkApiClient _apiClient;

    public AuthService(WatchSdkApiClient apiClient = null)
    {
        _apiClient = apiClient ?? new WatchSdkApiClient();
    }

    public bool IsConfigured => _apiClient.IsConfigured;

    public async Task RegisterAsync(string email, string password, string displayName = null)
    {
        ValidateCredentials(email, password);
        EnsureConfigured();

        var session = await _apiClient.RegisterAsync(email.Trim(), password, displayName);
        WatchSdkSessionStore.Save(session);
    }

    public async Task LoginAsync(string email, string password)
    {
        ValidateCredentials(email, password);
        EnsureConfigured();

        var session = await _apiClient.LoginAsync(email.Trim(), password);
        WatchSdkSessionStore.Save(session);
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (IsConfigured && WatchSdkSessionStore.HasSession)
            {
                await _apiClient.LogoutAsync();
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Logout request failed (local session will still be cleared): {ex.Message}");
        }
        finally
        {
            WatchSdkSessionStore.Clear();
        }
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync()
    {
        EnsureConfigured();
        return await _apiClient.GetCurrentUserAsync();
    }

    public void RestoreSessionOnColdStart()
    {
        if (!WatchSdkSessionStore.HasSession)
        {
            return;
        }

        if (WatchSdkSessionStore.IsRefreshTokenExpired())
        {
            WatchSdkSessionStore.Clear();
            OnSessionExpired?.Invoke();
        }
    }

    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            throw new WatchSdkApiException("Please enter a valid email address.");
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            throw new WatchSdkApiException("Password must be at least 6 characters.");
        }
    }

    private static void EnsureConfigured()
    {
        if (!WatchSdkConfig.IsConfigured)
        {
            throw new WatchSdkApiException(
                "Watch SDK backend is not configured. Copy Resources/WatchSdkConfig.example.json " +
                "to Resources/WatchSdkConfig.local.json and set apiBaseUrl.");
        }
    }

    public void Dispose()
    {
        _apiClient.Dispose();
    }
}
