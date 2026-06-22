#if UNITY
using UnityEngine;

public static partial class WatchSdkSessionStore
{
    private const string Prefix = "watchsdk.session.";

    private static partial StoredSessionDto ReadStoredSession()
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

    private static partial void WriteStoredSession(StoredSessionDto session)
    {
        PlayerPrefs.SetString(Prefix + "userId", session.userId ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "email", session.email ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "displayName", session.displayName ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "accessToken", session.accessToken ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "accessTokenExpiresOn", session.accessTokenExpiresOn ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "refreshToken", session.refreshToken ?? string.Empty);
        PlayerPrefs.SetString(Prefix + "refreshTokenExpiresOn", session.refreshTokenExpiresOn ?? string.Empty);
        PlayerPrefs.Save();
    }

    private static partial void DeleteStoredSession()
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
}
#endif
