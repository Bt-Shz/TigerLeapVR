using System;
using System.Threading.Tasks;

public sealed class PerformanceService : IDisposable
{
    private const string MahjongGameName = "Mahjong";
    private const string EasyHandGameName = "EasyHand";

    private readonly WatchSdkApiClient _apiClient;
    private readonly bool _ownsClient;

    public PerformanceService(WatchSdkApiClient apiClient = null, bool ownsClient = true)
    {
        if (apiClient == null)
        {
            _apiClient = new WatchSdkApiClient();
            _ownsClient = true;
        }
        else
        {
            _apiClient = apiClient;
            _ownsClient = ownsClient;
        }
    }

    public bool IsConfigured => _apiClient.IsConfigured;

    public Task UploadMahjongSessionAsync(int attempts, int failedAttempts, float timeTakenSeconds)
    {
        if (attempts <= 0)
        {
            throw new WatchSdkApiException(
                "Mahjong sessions need at least one attempt before upload.");
        }

        if (failedAttempts < 0 || failedAttempts > attempts)
        {
            throw new WatchSdkApiException(
                "Mahjong failed attempts must be between zero and total attempts.");
        }

        var accuracy = Math.Round(
            (attempts - failedAttempts) / (double)attempts * 100d,
            1);

        var averageTimeSeconds = ValidateAndRoundSeconds(
            timeTakenSeconds,
            "Mahjong completion time");
        return UploadGamePerformanceAsync(MahjongGameName, accuracy, averageTimeSeconds);
    }

    public Task UploadEasyHandSessionAsync(int cubesCaught, int currentMisses, float sessionSeconds)
    {
        if (cubesCaught < 0 || currentMisses < 0)
        {
            throw new WatchSdkApiException("EasyHand catches and misses cannot be negative.");
        }

        var total = (long)cubesCaught + currentMisses;
        if (total == 0)
        {
            throw new WatchSdkApiException(
                "EasyHand sessions need at least one catch or miss before upload.");
        }

        var accuracy = Math.Round(cubesCaught / (double)total * 100d, 1);

        var averageTimeSeconds = ValidateAndRoundSeconds(
            sessionSeconds,
            "EasyHand session duration");
        // averageTimeSeconds: Phase 2/3 stand-in = session duration; see docs/api-migration-phase3-easyhand-metrics.md
        return UploadGamePerformanceAsync(EasyHandGameName, accuracy, averageTimeSeconds);
    }

    private static int ValidateAndRoundSeconds(float seconds, string metricName)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
        {
            throw new WatchSdkApiException($"{metricName} must be a non-negative finite number.");
        }

        var rounded = Math.Round((double)seconds);
        if (rounded > int.MaxValue)
        {
            throw new WatchSdkApiException($"{metricName} is too large.");
        }

        return (int)rounded;
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
        if (_ownsClient)
        {
            _apiClient.Dispose();
        }
    }
}
