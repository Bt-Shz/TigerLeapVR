#if !UNITY
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static partial class WatchSdkConfig
{
    private static string _explicitConfigPath;

    public static void Initialize(string configPath = null)
    {
        _explicitConfigPath = configPath;
        Reload();
    }

    private static partial string LoadBaseUrl()
    {
        foreach (var candidate in EnumerateConfigCandidates())
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            var json = File.ReadAllText(candidate);
            var apiBaseUrl = ParseConfigFile(json, candidate);
            if (!string.IsNullOrEmpty(apiBaseUrl))
            {
                return NormalizeBaseUrl(apiBaseUrl);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateConfigCandidates()
    {
        if (!string.IsNullOrWhiteSpace(_explicitConfigPath))
        {
            yield return Path.GetFullPath(_explicitConfigPath);
        }

        var envPath = Environment.GetEnvironmentVariable("WATCHSDK_CONFIG");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return Path.GetFullPath(envPath);
        }

        yield return Path.GetFullPath("./WatchSdkConfig.local.json");
        yield return Path.GetFullPath("./WatchSdkConfig.example.json");
        yield return Path.GetFullPath("./Assets/Resources/WatchSdkConfig.local.json");
        yield return Path.GetFullPath("./Assets/Resources/WatchSdkConfig.example.json");
    }

    private static partial WatchSdkConfigFile DeserializeConfig(string json) =>
        JsonConvert.DeserializeObject<WatchSdkConfigFile>(json);

    private static partial void LogConfigParseWarning(string sourceDescription, string message) =>
        Console.Error.WriteLine($"Failed to parse {sourceDescription}: {message}");
}
#endif
