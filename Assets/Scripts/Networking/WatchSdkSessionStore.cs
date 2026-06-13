using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Persists auth session in PlayerPrefs (mirrors Flutter AppSessionService fields).
/// </summary>
public static class WatchSdkSessionStore
{
    private const string Prefix = "watchsdk.session.";

    public static bool HasSession => !string.IsNullOrEmpty(GetRefreshToken());

    public static bool IsRefreshTokenExpired()
    {
        var expiresOn = GetRefreshTokenExpiresOn();
        if (!expiresOn.HasValue)
        {
            return true;
        }

        return DateTime.UtcNow >= expiresOn.Value.ToUniversalTime();
    }

    public static bool IsSignedIn()
    {
        if (!HasSession)
        {
            return false;
        }

        return !IsRefreshTokenExpired();
    }

    public static StoredSessionDto Load()
    {
        var userId = PlayerPrefs.GetString(Prefix + "userId", string.Empty);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return new StoredSessionDto
        {
            userId = userId,
            email = PlayerPrefs.GetString(Prefix + "email", string.Empty),
            displayName = PlayerPrefs.GetString(Prefix + "displayName", string.Empty),
            accessToken = PlayerPrefs.GetString(Prefix + "accessToken", string.Empty),
            accessTokenExpiresOn = PlayerPrefs.GetString(Prefix + "accessTokenExpiresOn", string.Empty),
            refreshToken = PlayerPrefs.GetString(Prefix + "refreshToken", string.Empty),
            refreshTokenExpiresOn = PlayerPrefs.GetString(Prefix + "refreshTokenExpiresOn", string.Empty),
        };
    }

    public static void Save(AuthResponseDto response)
    {
        if (response?.user == null)
        {
            throw new WatchSdkApiException("Invalid auth response: missing user.");
        }

        PlayerPrefs.SetString(Prefix + "userId", response.user.userId ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "email", response.user.email ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "displayName", response.user.displayName ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "accessToken", response.accessToken ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "accessTokenExpiresOn", response.accessTokenExpiresOn ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "refreshToken", response.refreshToken ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "refreshTokenExpiresOn", response.refreshTokenExpiresOn ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Prefix + "userId");
        PlayerPrefs.DeleteKey(Prefix + "email");
        PlayerPrefs.DeleteKey(Prefix + "displayName");
        PlayerPrefs.DeleteKey(Prefix + "accessToken");
        PlayerPrefs.DeleteKey(Prefix + "accessTokenExpiresOn");
        PlayerPrefs.DeleteKey(Prefix + "refreshToken");
        PlayerPrefs.DeleteKey(Prefix + "refreshTokenExpiresOn");
        PlayerPrefs.Save();
    }

    public static string GetAccessToken() =>
        PlayerPrefs.GetString(Prefix + "accessToken", string.Empty);

    public static string GetRefreshToken() =>
        PlayerPrefs.GetString(Prefix + "refreshToken", string.Empty);

    public static string GetUserEmail() =>
        PlayerPrefs.GetString(Prefix + "email", string.Empty);

    public static string GetUserId() =>
        PlayerPrefs.GetString(Prefix + "userId", string.Empty);

    public static string GetDisplayName() =>
        PlayerPrefs.GetString(Prefix + "displayName", string.Empty);

    public static DateTime? GetAccessTokenExpiresOn() =>
        ParseDateTime(PlayerPrefs.GetString(Prefix + "accessTokenExpiresOn", string.Empty));

    public static DateTime? GetRefreshTokenExpiresOn() =>
        ParseDateTime(PlayerPrefs.GetString(Prefix + "refreshTokenExpiresOn", string.Empty));

    private static DateTime? ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
