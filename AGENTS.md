# AGENTS.md - TigerLeapVR

Unity 6000.0.68f1 VR client. Sibling backend/mobile repo: `../watchSDK`.

Before edits, run `git status -sb` in this repo. Preserve unrelated local changes.

## Backend contract

- Backend source of truth: `../watchSDK/backend/WatchSdk.Api`.
- Flutter/mobile source of truth for reader behavior: `../watchSDK/lib/services/watch_sdk_api_client.dart` and performance UI/models.
- TigerLeapVR is an HTTP client only. It does not host PostgreSQL, issue JWTs, or store server secrets.
- VR writes game performance to WatchSdk API; mobile reads it from the same API.
- API changes require checking both `Assets/Scripts/Networking/` and `tools/WatchSdkCli/`.
- Checked OpenAPI artifact lives at `../watchSDK/docs/openapi.json`; refresh it from `watchSDK` after API shape changes with `bash scripts/export-openapi.sh`.

## Networking rules

- Gameplay/UI scripts call `BackendFacade`.
- Do not call `WatchSdkApiClient` directly from gameplay scripts.
- Reuse `AuthService`, `PerformanceService`, `WatchSdkApiClient`, and DTOs; do not duplicate HTTP or payload math.
- API game keys are exact: `Mahjong` and `EasyHand`.
- EasyHand scenes/UI may say Taichi, but the API key is always `EasyHand`.
- EasyHand `averageTimeSeconds` currently means total session duration, not per-action average.
- Mahjong `averageTimeSeconds` means rounded completion time.
- No Firebase reintroduction: no Firebase SDK, config, managers, analytics stubs, or scene objects.

## Config and secrets

- Committed example config: `Assets/Resources/WatchSdkConfig.example.json`.
- Local override is gitignored: `Assets/Resources/WatchSdkConfig.local.json`.
- CLI session file is gitignored: `.watchsdk-session.json`.
- Never commit JWT keys, database strings, Azure keys, local API overrides, or tokens.

## CLI

`tools/WatchSdkCli` is the non-Unity smoke harness for auth/session/performance uploads.

- Build: `dotnet build --no-restore tools/WatchSdkCli/WatchSdkCli.csproj`
- CLI composes `AuthService`, `PerformanceService`, and `WatchSdkApiClient` directly, mirroring `BackendFacade.Awake()`.
- Upload commands must call service methods, not hand-build PUT JSON.

## Verification

| Change | Run |
|--------|-----|
| Networking/API/CLI | `dotnet build --no-restore tools/WatchSdkCli/WatchSdkCli.csproj` |
| Scene, prefab, asmdef, or Unity compile path | Unity compile in editor |
| API contract changed in `watchSDK` | backend tests + Flutter tests there, then VR CLI build here |
| Local API credentials/config available | `watchsdk-cli config check`, then login/upload smoke against local API |

Ignored `docs/` and `.cursor/` files may exist as history/deep specs. Durable agent routing belongs in this file and the workspace `../AGENTS.md`.
