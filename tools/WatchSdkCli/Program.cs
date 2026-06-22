using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace WatchSdkCli;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitValidation = 1;
    private const int ExitApi = 2;
    private const int ExitUnexpected = 3;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return ExitValidation;
        }

        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintHelp();
            return ExitSuccess;
        }

        if (options.ParseError != null)
        {
            Console.Error.WriteLine(options.ParseError);
            PrintHelp();
            return ExitValidation;
        }

        WatchSdkConfig.Initialize(options.ConfigPath);
        WatchSdkSessionStore.Initialize(options.SessionPath);

        using var host = new CliHost { Verbose = options.Verbose };

        try
        {
            return await DispatchAsync(host, options);
        }
        catch (WatchSdkApiException ex)
        {
            WriteApiError(ex);
            return ExitApi;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitUnexpected;
        }
    }

    private static async Task<int> DispatchAsync(CliHost host, CliOptions options)
    {
        switch (options.Command)
        {
            case "config":
                return await RunConfigAsync(host, options);
            case "register":
                return await RunRegisterAsync(host, options);
            case "login":
                return await RunLoginAsync(host, options);
            case "logout":
                return await RunLogoutAsync(host);
            case "session":
                return RunSession(host);
            case "me":
                return await RunMeAsync(host);
            case "upload":
                return await RunUploadAsync(host, options);
            default:
                Console.Error.WriteLine($"Unknown command: {options.Command}");
                PrintHelp();
                return ExitValidation;
        }
    }

    private static Task<int> RunConfigAsync(CliHost host, CliOptions options)
    {
        if (options.Subcommand != "check")
        {
            Console.Error.WriteLine("Usage: watchsdk-cli config check [--config <path>]");
            return Task.FromResult(ExitValidation);
        }

        if (!host.IsConfigured)
        {
            Console.Error.WriteLine(
                "Watch SDK backend is not configured. Copy Assets/Resources/WatchSdkConfig.example.json " +
                "to WatchSdkConfig.local.json and set apiBaseUrl.");
            return Task.FromResult(ExitValidation);
        }

        Console.WriteLine($"Configured: {host.NormalizedBaseUrl}");
        return Task.FromResult(ExitSuccess);
    }

    private static async Task<int> RunRegisterAsync(CliHost host, CliOptions options)
    {
        var email = options.GetRequired("email");
        var password = ResolvePassword(options);
        var displayName = options.GetOptional("display-name");

        await host.RegisterAsync(email, password, displayName);
        Console.WriteLine($"Registered and signed in as {WatchSdkSessionStore.GetUserEmail()}.");
        if (host.Verbose)
        {
            Console.Error.WriteLine($"Session saved to {WatchSdkSessionStore.SessionFilePath}");
        }

        return ExitSuccess;
    }

    private static async Task<int> RunLoginAsync(CliHost host, CliOptions options)
    {
        var email = options.GetRequired("email");
        var password = ResolvePassword(options);

        await host.LoginAsync(email, password);
        Console.WriteLine($"Signed in as {WatchSdkSessionStore.GetUserEmail()}.");
        if (host.Verbose)
        {
            Console.Error.WriteLine($"Session saved to {WatchSdkSessionStore.SessionFilePath}");
        }

        return ExitSuccess;
    }

    private static async Task<int> RunLogoutAsync(CliHost host)
    {
        await host.LogoutAsync();
        Console.WriteLine("Signed out.");
        return ExitSuccess;
    }

    private static int RunSession(CliHost host)
    {
        Console.WriteLine(host.GetSessionInfo());
        if (host.Verbose && host.IsSignedIn)
        {
            Console.Error.WriteLine($"Session file: {WatchSdkSessionStore.SessionFilePath}");
        }

        return ExitSuccess;
    }

    private static async Task<int> RunMeAsync(CliHost host)
    {
        if (!host.IsSignedIn)
        {
            Console.Error.WriteLine("Not signed in. Run login or register first.");
            return ExitValidation;
        }

        var user = await host.GetCurrentUserAsync();
        Console.WriteLine(
            $"userId={user.userId} email={user.email} displayName={user.displayName ?? "(none)"}");
        return ExitSuccess;
    }

    private static async Task<int> RunUploadAsync(CliHost host, CliOptions options)
    {
        if (!host.IsSignedIn)
        {
            Console.Error.WriteLine("Sign in to upload game results.");
            return ExitValidation;
        }

        switch (options.Subcommand)
        {
            case "mahjong":
            case "upload-mahjong":
                var attempts = options.GetRequiredInt("attempts");
                var failed = options.GetRequiredInt("failed");
                var mahjongSeconds = options.GetRequiredFloat("seconds");
                await host.UploadMahjongAsync(attempts, failed, mahjongSeconds);
                return ExitSuccess;

            case "easyhand":
            case "upload-easyhand":
                var caught = options.GetRequiredInt("caught");
                var misses = options.GetRequiredInt("misses");
                var easyHandSeconds = options.GetRequiredFloat("seconds");
                await host.UploadEasyHandAsync(caught, misses, easyHandSeconds);
                return ExitSuccess;

            default:
                Console.Error.WriteLine(
                    "Usage: watchsdk-cli upload mahjong --attempts <n> --failed <n> --seconds <n>");
                Console.Error.WriteLine(
                    "       watchsdk-cli upload easyhand --caught <n> --misses <n> --seconds <n>");
                return ExitValidation;
        }
    }

    private static string ResolvePassword(CliOptions options)
    {
        var password = options.GetOptional("password");
        if (!string.IsNullOrEmpty(password))
        {
            return password;
        }

        password = Environment.GetEnvironmentVariable("WATCHSDK_PASSWORD");
        if (!string.IsNullOrEmpty(password))
        {
            return password;
        }

        throw new WatchSdkApiException("Password is required via --password or WATCHSDK_PASSWORD.");
    }

    private static void WriteApiError(WatchSdkApiException ex)
    {
        if (ex.StatusCode.HasValue)
        {
            Console.Error.WriteLine($"API error ({ex.StatusCode.Value}): {ex.Message}");
        }
        else
        {
            Console.Error.WriteLine(ex.Message);
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            watchsdk-cli — TigerLeapVR backend integration harness

            Global options:
              --config <path>     API config JSON (or set WATCHSDK_CONFIG)
              --session <path>    Session file (default: ~/.config/tigerleap-vr/session.json)
              -v, --verbose       Log computed payloads and session paths (never passwords/tokens)

            Commands:
              config check        Print normalized apiBaseUrl
              register            --email <addr> --password <pwd> [--display-name <name>]
              login               --email <addr> --password <pwd>
              logout              Clear local session and call API logout
              session             Print current session status
              me                  GET /api/v1/me (requires sign-in)
              upload mahjong      --attempts <n> --failed <n> --seconds <n>
              upload easyhand     --caught <n> --misses <n> --seconds <n>

            Password: pass --password or set WATCHSDK_PASSWORD (not logged).

            Examples:
              dotnet run -- config check --config ../../Assets/Resources/WatchSdkConfig.example.json
              dotnet run -- register --email test@example.com --password 'secret12'
              dotnet run -- login --email test@example.com --password 'secret12'
              dotnet run -- me
              dotnet run -- upload mahjong --attempts 10 --failed 2 --seconds 45.7
              dotnet run -- upload easyhand --caught 8 --misses 2 --seconds 312.4
              dotnet run -- logout
            """);
    }
}

internal sealed class CliOptions
{
    public string Command { get; private set; }
    public string Subcommand { get; private set; }
    public string ConfigPath { get; private set; }
    public string SessionPath { get; private set; }
    public bool Verbose { get; private set; }
    public bool ShowHelp { get; private set; }
    public string ParseError { get; private set; }

    private readonly Dictionary<string, string> _values =
        new(StringComparer.OrdinalIgnoreCase);

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        var positionals = new List<string>();
        var i = 0;

        while (i < args.Length)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    return options;
                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    i++;
                    continue;
                case "--config":
                    if (!TryReadValue(args, ref i, out var configPath))
                    {
                        options.ParseError = "--config requires a path.";
                        return options;
                    }

                    options.ConfigPath = configPath;
                    continue;
                case "--session":
                    if (!TryReadValue(args, ref i, out var sessionPath))
                    {
                        options.ParseError = "--session requires a path.";
                        return options;
                    }

                    options.SessionPath = sessionPath;
                    continue;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        var eq = arg.IndexOf('=', StringComparison.Ordinal);
                        if (eq > 2)
                        {
                            var key = arg.Substring(2, eq - 2);
                            var value = arg.Substring(eq + 1);
                            options._values[key] = value;
                            i++;
                            continue;
                        }

                        var flag = arg.Substring(2);
                        if (!TryReadValue(args, ref i, out var flagValue))
                        {
                            options.ParseError = $"{arg} requires a value.";
                            return options;
                        }

                        options._values[flag] = flagValue;
                        continue;
                    }

                    positionals.Add(arg);
                    i++;
                    continue;
            }
        }

        if (positionals.Count == 0)
        {
            options.ParseError = "Command is required.";
            return options;
        }

        options.Command = positionals[0].ToLowerInvariant();
        if (positionals.Count > 1)
        {
            options.Subcommand = positionals[1].ToLowerInvariant();
        }

        return options;
    }

    public string GetRequired(string key)
    {
        if (_values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new WatchSdkApiException($"Missing required option: --{key}");
    }

    public string GetOptional(string key) =>
        _values.TryGetValue(key, out var value) ? value : null;

    public int GetRequiredInt(string key)
    {
        var text = GetRequired(key);
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new WatchSdkApiException($"Invalid integer for --{key}: {text}");
        }

        return value;
    }

    public float GetRequiredFloat(string key)
    {
        var text = GetRequired(key);
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new WatchSdkApiException($"Invalid number for --{key}: {text}");
        }

        return value;
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            return false;
        }

        index += 2;
        value = args[index - 1];
        return true;
    }
}
