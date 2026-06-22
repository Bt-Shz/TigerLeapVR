using System;
using System.Threading.Tasks;

namespace WatchSdkCli;

public sealed class CliHost : IDisposable
{
    private readonly WatchSdkApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly PerformanceService _performanceService;

    public CliHost()
    {
        _apiClient = new WatchSdkApiClient();
        _authService = new AuthService(_apiClient, ownsClient: false);
        _performanceService = new PerformanceService(_apiClient, ownsClient: false);
        _authService.OnSessionExpired += HandleSessionExpired;
        _authService.RestoreSessionOnColdStart();
    }

    public bool Verbose { get; set; }

    public bool IsConfigured => _authService.IsConfigured;

    public string NormalizedBaseUrl => WatchSdkConfig.NormalizedBaseUrl;

    public bool IsSignedIn => WatchSdkSessionStore.IsSignedIn();

    public string GetSessionInfo()
    {
        if (!IsSignedIn)
        {
            return "No active session";
        }

        var refreshExpires = WatchSdkSessionStore.GetRefreshTokenExpiresOn();
        if (refreshExpires.HasValue)
        {
            var remaining = refreshExpires.Value.ToUniversalTime() - DateTime.UtcNow;
            if (remaining.TotalSeconds > 0)
            {
                return
                    $"Session active for {WatchSdkSessionStore.GetUserEmail()}. " +
                    $"Refresh expires in {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m";
            }
        }

        return $"Session active for {WatchSdkSessionStore.GetUserEmail()}";
    }

    public Task RegisterAsync(string email, string password, string displayName) =>
        _authService.RegisterAsync(email, password, displayName);

    public Task LoginAsync(string email, string password) =>
        _authService.LoginAsync(email, password);

    public Task LogoutAsync() => _authService.LogoutAsync();

    public Task<AuthenticatedUserDto> GetCurrentUserAsync() =>
        _authService.GetCurrentUserAsync();

    public async Task UploadMahjongAsync(int attempts, int failedAttempts, float seconds)
    {
        var accuracy = attempts > 0
            ? Math.Round((attempts - failedAttempts) / (double)attempts * 100d, 1)
            : 0d;
        var averageTimeSeconds = Math.Max(0, (int)Math.Round(seconds));

        if (Verbose)
        {
            Console.Error.WriteLine(
                $"Uploading Mahjong (accuracy={accuracy}, averageTimeSeconds={averageTimeSeconds})");
        }

        await _performanceService.UploadMahjongSessionAsync(attempts, failedAttempts, seconds);
        Console.WriteLine(
            $"Mahjong performance uploaded (accuracy={accuracy}, averageTimeSeconds={averageTimeSeconds}).");
    }

    public async Task UploadEasyHandAsync(int caught, int misses, float seconds)
    {
        var total = caught + misses;
        var accuracy = total > 0
            ? Math.Round(caught / (double)total * 100d, 1)
            : 0d;
        var averageTimeSeconds = Math.Max(0, (int)Math.Round(seconds));

        if (Verbose)
        {
            Console.Error.WriteLine(
                $"Uploading EasyHand (accuracy={accuracy}, averageTimeSeconds={averageTimeSeconds})");
        }

        await _performanceService.UploadEasyHandSessionAsync(caught, misses, seconds);
        Console.WriteLine(
            $"EasyHand performance uploaded (accuracy={accuracy}, averageTimeSeconds={averageTimeSeconds}).");
    }

    private void HandleSessionExpired()
    {
        if (Verbose)
        {
            Console.Error.WriteLine("Session expired.");
        }
    }

    public void Dispose()
    {
        _authService.OnSessionExpired -= HandleSessionExpired;
        _authService.Dispose();
        _performanceService.Dispose();
        _apiClient.Dispose();
    }
}
