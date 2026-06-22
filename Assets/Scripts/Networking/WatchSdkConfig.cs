using System;

/// <summary>
/// Loads the WatchSdk API base URL from Resources JSON (Unity) or a config file (CLI).
/// Copy WatchSdkConfig.example.json to WatchSdkConfig.local.json (gitignored) for local dev.
/// </summary>
public static partial class WatchSdkConfig
{
    private static string _cachedBaseUrl;
    private static bool _loaded;

    public static bool IsConfigured => !string.IsNullOrEmpty(NormalizedBaseUrl);

    public static string NormalizedBaseUrl
    {
        get
        {
            EnsureLoaded();
            return _cachedBaseUrl;
        }
    }

    public static void Reload()
    {
        _loaded = false;
        _cachedBaseUrl = null;
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _cachedBaseUrl = LoadBaseUrl();
    }

    private static partial string LoadBaseUrl();

    private static string ParseConfigFile(string json, string sourceDescription)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var data = DeserializeConfig(json);
            return data?.apiBaseUrl;
        }
        catch (Exception ex)
        {
            LogConfigParseWarning(sourceDescription, ex.Message);
            return null;
        }
    }

    private static partial WatchSdkConfigFile DeserializeConfig(string json);

    private static partial void LogConfigParseWarning(string sourceDescription, string message);

    public static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.EndsWith("/") ? trimmed : trimmed + "/";
    }

    public static Uri BuildUri(string path)
    {
        var baseUrl = NormalizedBaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new WatchSdkApiException(
                "Watch SDK backend is not configured. Copy Resources/WatchSdkConfig.example.json " +
                "to Resources/WatchSdkConfig.local.json and set apiBaseUrl.");
        }

        var normalizedPath = path.StartsWith("/") ? path.Substring(1) : path;
        return new Uri(new Uri(baseUrl), normalizedPath);
    }

    [Serializable]
    internal class WatchSdkConfigFile
    {
        public string apiBaseUrl;
    }
}
