using System;
using System.Threading.Tasks;

public sealed class PerformanceService : IDisposable
{
    private const string MahjongGameName = "Mahjong";
    private const string EasyHandGameName = "EasyHand";

    private readonly WatchSdkApiClient _apiClient;

    public PerformanceService(WatchSdkApiClient apiClient = null)
    {
        _apiClient = apiClient ?? new WatchSdkApiClient();
    }

    public bool IsConfigured => _apiClient.IsConfigured;

    public Task UploadMahjongSessionAsync(int attempts, int failedAttempts, float timeTakenSeconds)
    {
        var accuracy = attempts > 0
            ? Math.Round((attempts - failedAttempts) / (double)attempts * 100d, 1)
            : 0d;

        var averageTimeSeconds = Math.Max(0, (int)Math.Round(timeTakenSeconds));
        return UploadGamePerformanceAsync(MahjongGameName, accuracy, averageTimeSeconds);
    }

    public Task UploadEasyHandSessionAsync(int cubesCaught, int currentMisses, float sessionSeconds)
    {
        var total = cubesCaught + currentMisses;
        var accuracy = total > 0
            ? Math.Round(cubesCaught / (double)total * 100d, 1)
            : 0d;

        var averageTimeSeconds = Math.Max(0, (int)Math.Round(sessionSeconds));
        return UploadGamePerformanceAsync(EasyHandGameName, accuracy, averageTimeSeconds);
    }

    private async Task UploadGamePerformanceAsync(
        string gameName,
        double accuracy,
        int averageTimeSeconds)
    {
        if (!WatchSdkConfig.IsConfigured)
        {
            throw new WatchSdkApiException(
                "Watch SDK backend is not configured. Copy Resources/WatchSdkConfig.example.json " +
                "to Resources/WatchSdkConfig.local.json and set apiBaseUrl.");
        }

        var body = new GamePerformanceUploadDto
        {
            accuracy = accuracy,
            averageTimeSeconds = averageTimeSeconds,
            monthlyPerformance = Array.Empty<MonthlyGamePerformanceDto>(),
            lastUpdated = DateTime.UtcNow.ToString("o"),
        };

        await _apiClient.PutGamePerformanceAsync(gameName, body);
    }

    public void Dispose()
    {
        _apiClient.Dispose();
    }
}
