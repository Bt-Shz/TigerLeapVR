using System;
using System.Globalization;

/// <summary>
/// Persists auth session (PlayerPrefs in Unity, JSON file in CLI).
/// Mirrors Flutter AppSessionService fields.
/// </summary>
public static partial class WatchSdkSessionStore
{
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
        return ReadStoredSession();
    }

    public static void Save(AuthResponseDto response)
    {
        if (response?.user == null)
        {
            throw new WatchSdkApiException("Invalid auth response: missing user.");
        }

        WriteStoredSession(new StoredSessionDto
        {
            userId = response.user.userId ?? string.Empty,
            email = response.user.email ?? string.Empty,
            displayName = response.user.displayName ?? string.Empty,
            accessToken = response.accessToken ?? string.Empty,
            accessTokenExpiresOn = response.accessTokenExpiresOn ?? string.Empty,
            refreshToken = response.refreshToken ?? string.Empty,
            refreshTokenExpiresOn = response.refreshTokenExpiresOn ?? string.Empty,
        });
    }

    public static void Clear() => DeleteStoredSession();

    public static string GetAccessToken() => ReadField(session => session?.accessToken);

    public static string GetRefreshToken() => ReadField(session => session?.refreshToken);

    public static string GetUserEmail() => ReadField(session => session?.email);

    public static string GetUserId() => ReadField(session => session?.userId);

    public static string GetDisplayName() => ReadField(session => session?.displayName);

    public static DateTime? GetAccessTokenExpiresOn() =>
        ParseDateTime(ReadField(session => session?.accessTokenExpiresOn));

    public static DateTime? GetRefreshTokenExpiresOn() =>
        ParseDateTime(ReadField(session => session?.refreshTokenExpiresOn));

    private static string ReadField(Func<StoredSessionDto, string> selector)
    {
        var session = ReadStoredSession();
        return selector(session) ?? string.Empty;
    }

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

    private static partial StoredSessionDto ReadStoredSession();

    private static partial void WriteStoredSession(StoredSessionDto session);

    private static partial void DeleteStoredSession();
}
