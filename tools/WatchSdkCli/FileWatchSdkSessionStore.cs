#if !UNITY
using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

public static partial class WatchSdkSessionStore
{
    private static string _sessionFilePath;
    private static StoredSessionDto _cachedSession;
    private static bool _loaded;

    public static void Initialize(string sessionPath = null)
    {
        _sessionFilePath = string.IsNullOrWhiteSpace(sessionPath)
            ? GetDefaultSessionPath()
            : Path.GetFullPath(sessionPath);

        _loaded = false;
        _cachedSession = null;
    }

    public static string SessionFilePath =>
        _sessionFilePath ?? GetDefaultSessionPath();

    private static partial StoredSessionDto ReadStoredSession()
    {
        EnsureLoaded();
        return _cachedSession;
    }

    private static partial void WriteStoredSession(StoredSessionDto session)
    {
        EnsureSessionDirectory();
        var json = JsonConvert.SerializeObject(session, Formatting.Indented);
        File.WriteAllText(_sessionFilePath, json);
        ApplyUserOnlyPermissions(_sessionFilePath);
        _cachedSession = session;
        _loaded = true;
    }

    private static partial void DeleteStoredSession()
    {
        if (File.Exists(_sessionFilePath))
        {
            File.Delete(_sessionFilePath);
        }

        _cachedSession = null;
        _loaded = true;
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (string.IsNullOrWhiteSpace(_sessionFilePath))
        {
            _sessionFilePath = GetDefaultSessionPath();
        }

        if (!File.Exists(_sessionFilePath))
        {
            _cachedSession = null;
            return;
        }

        try
        {
            var json = File.ReadAllText(_sessionFilePath);
            _cachedSession = JsonConvert.DeserializeObject<StoredSessionDto>(json);
            if (_cachedSession == null || string.IsNullOrEmpty(_cachedSession.refreshToken))
            {
                _cachedSession = null;
            }
        }
        catch
        {
            _cachedSession = null;
        }
    }

    private static void EnsureSessionDirectory()
    {
        var directory = Path.GetDirectoryName(_sessionFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetDefaultSessionPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(configHome))
        {
            return Path.Combine(configHome, "tigerleap-vr", "session.json");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "tigerleap-vr", "session.json");
        }

        return Path.Combine(home, ".config", "tigerleap-vr", "session.json");
    }

    private static void ApplyUserOnlyPermissions(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
#endif
