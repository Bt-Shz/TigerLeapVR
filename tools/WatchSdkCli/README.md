# WatchSdk CLI

Standalone .NET console harness for TigerLeapVR backend integration. Exercises the same `AuthService`, `PerformanceService`, and `WatchSdkApiClient` classes as the Unity VR game — without Unity or a headset.

## Build

```bash
cd tools/WatchSdkCli
dotnet build
```

## Config

Copy the example config and set your API base URL:

```bash
cp ../../Assets/Resources/WatchSdkConfig.example.json ../../Assets/Resources/WatchSdkConfig.local.json
# Edit apiBaseUrl — e.g. http://127.0.0.1:5000 for local watchSDK API
```

The CLI resolves config from (first match wins):

1. `--config <path>`
2. `WATCHSDK_CONFIG` environment variable
3. `./WatchSdkConfig.local.json`
4. `./WatchSdkConfig.example.json`
5. `./Assets/Resources/WatchSdkConfig.local.json`
6. `./Assets/Resources/WatchSdkConfig.example.json`

## Session

Signed-in tokens are stored at `~/.config/tigerleap-vr/session.json` by default (override with `--session`). The file is created with mode `0600` on Unix.

## Commands

```bash
# Check API URL
dotnet run -- config check --config ../../Assets/Resources/WatchSdkConfig.example.json

# Register / login (password via --password or WATCHSDK_PASSWORD)
dotnet run -- register --email test@example.com --password 'secret12'
dotnet run -- login --email test@example.com --password 'secret12'

# Session info and current user
dotnet run -- session
dotnet run -- me

# Upload game performance (same payloads as VR)
dotnet run -- upload mahjong --attempts 10 --failed 2 --seconds 45.7
dotnet run -- upload easyhand --caught 8 --misses 2 --seconds 312.4

dotnet run -- logout
```

Add `-v` / `--verbose` for computed accuracy and session file paths (never logs passwords or tokens).

## Local API

Start the watchSDK backend from the sibling repo:

```bash
cd ../watchSDK && ./run-backend
```

Swagger: http://127.0.0.1:5000/swagger

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Validation error (bad args, not configured, not signed in) |
| 2 | API error (`WatchSdkApiException`) |
| 3 | Unexpected error |
