#if UNITY
using System;
using UnityEngine;

public static partial class WatchSdkConfig
{
    private const string LocalResourceName = "WatchSdkConfig.local";
    private const string ExampleResourceName = "WatchSdkConfig.example";

    private static partial string LoadBaseUrl()
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

        return ParseConfigFile(asset.text, resourceName);
    }

    private static partial WatchSdkConfigFile DeserializeConfig(string json) =>
        JsonUtility.FromJson<WatchSdkConfigFile>(json);

    private static partial void LogConfigParseWarning(string sourceDescription, string message) =>
        Debug.LogWarning($"Failed to parse {sourceDescription}: {message}");
}
#endif
