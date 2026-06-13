using System;
using UnityEngine;

/// <summary>
/// Loads the WatchSdk API base URL from Resources JSON.
/// Copy WatchSdkConfig.example.json to WatchSdkConfig.local.json (gitignored) for local dev.
/// </summary>
public static class WatchSdkConfig
{
    private const string LocalResourceName = "WatchSdkConfig.local";
    private const string ExampleResourceName = "WatchSdkConfig.example";

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

    private static string LoadBaseUrl()
    {
        var local = LoadFromResource(LocalResourceName);
        if (!string.IsNullOrEmpty(local))
        {
            return NormalizeBaseUrl(local);
        }

        var example = LoadFromResource(ExampleResourceName);
        if (!string.IsNullOrEmpty(example))
        {
            return NormalizeBaseUrl(example);
        }

        return null;
    }

    private static string LoadFromResource(string resourceName)
    {
        var asset = Resources.Load<TextAsset>(resourceName);
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
        {
            return null;
        }

        try
        {
            var data = JsonUtility.FromJson<WatchSdkConfigFile>(asset.text);
            return data?.apiBaseUrl;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse {resourceName}: {ex.Message}");
            return null;
        }
    }

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
    private class WatchSdkConfigFile
    {
        public string apiBaseUrl;
    }
}
