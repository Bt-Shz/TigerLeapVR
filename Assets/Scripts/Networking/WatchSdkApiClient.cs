using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class WatchSdkApiClient : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    public WatchSdkApiClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = RequestTimeout,
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool IsConfigured => WatchSdkConfig.IsConfigured;

    public async Task<AuthResponseDto> RegisterAsync(string email, string password, string displayName = null)
    {
        var body = new AuthRequestDto
        {
            email = email,
            password = password,
            displayName = displayName,
        };

        using (var response = await SendJsonAsync(HttpMethod.Post, "/api/v1/auth/register", body))
        {
            return ParseAuthResponse(response);
        }
    }

    public async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        var body = new AuthRequestDto
        {
            email = email,
            password = password,
        };

        using (var response = await SendJsonAsync(HttpMethod.Post, "/api/v1/auth/login", body))
        {
            return ParseAuthResponse(response);
        }
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var body = new RefreshRequestDto { refreshToken = refreshToken };
        using (var response = await SendJsonAsync(HttpMethod.Post, "/api/v1/auth/refresh", body))
        {
            return ParseAuthResponse(response);
        }
    }

    public async Task LogoutAsync()
    {
        var accessToken = WatchSdkSessionStore.GetAccessToken();
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        await SendAuthorizedJsonAsync(HttpMethod.Post, "/api/v1/auth/logout", allowRefresh: false);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync()
    {
        var json = await GetAuthorizedJsonAsync("/api/v1/me");
        return json.ToObject<AuthenticatedUserDto>();
    }

    public async Task PutGamePerformanceAsync(string gameName, GamePerformanceUploadDto body)
    {
        var encoded = Uri.EscapeDataString(gameName.Trim());
        await SendAuthorizedJsonAsync(
            HttpMethod.Put,
            $"/api/v1/me/performance/games/{encoded}",
            body);
    }

    private async Task<JObject> GetAuthorizedJsonAsync(string path)
    {
        var text = await SendAuthorizedJsonAsync(HttpMethod.Get, path);
        return ParseJsonObject(text);
    }

    private async Task<string> SendAuthorizedJsonAsync(
        HttpMethod method,
        string path,
        object body = null,
        bool allowRefresh = true)
    {
        var accessToken = WatchSdkSessionStore.GetAccessToken();
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new WatchSdkApiException(
                "No backend access token is available. Sign in again.");
        }

        using (var response = await SendJsonAsync(
                   method,
                   path,
                   body,
                   bearerToken: accessToken,
                   throwOnError: false))
        {
            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized || !allowRefresh)
            {
                EnsureSuccess(response);
                return await response.Content.ReadAsStringAsync();
            }
        }

        await RefreshWithMutexAsync();

        var retryToken = WatchSdkSessionStore.GetAccessToken();
        using (var retryResponse = await SendJsonAsync(
                   method,
                   path,
                   body,
                   bearerToken: retryToken))
        {
            return await retryResponse.Content.ReadAsStringAsync();
        }
    }

    private async Task RefreshWithMutexAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var refreshToken = WatchSdkSessionStore.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                WatchSdkSessionStore.Clear();
                throw new WatchSdkApiException("No refresh token is available for the current session.");
            }

            AuthResponseDto session;
            try
            {
                session = await RefreshAsync(refreshToken);
            }
            catch (WatchSdkApiException ex) when (ex.StatusCode is 400 or 401)
            {
                WatchSdkSessionStore.Clear();
                throw;
            }

            WatchSdkSessionStore.Save(session);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendJsonAsync(
        HttpMethod method,
        string path,
        object body = null,
        string bearerToken = null,
        bool throwOnError = true)
    {
        if (!WatchSdkConfig.IsConfigured)
        {
            throw new WatchSdkApiException(
                "Watch SDK backend is not configured. Copy Resources/WatchSdkConfig.example.json " +
                "to Resources/WatchSdkConfig.local.json and set apiBaseUrl.");
        }

        var uri = WatchSdkConfig.BuildUri(path);
        using var request = new HttpRequestMessage(method, uri);

        if (body != null)
        {
            var json = JsonConvert.SerializeObject(body, JsonSettings);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            throw new WatchSdkApiException(BuildTimeoutMessage(uri));
        }

        if (throwOnError)
        {
            try
            {
                EnsureSuccess(response);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        return response;
    }

    private static AuthResponseDto ParseAuthResponse(HttpResponseMessage response)
    {
        EnsureSuccess(response);
        var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var json = ParseJsonObject(text);
        var session = json.ToObject<AuthResponseDto>();
        if (session?.user == null || string.IsNullOrEmpty(session.accessToken))
        {
            throw new WatchSdkApiException("Invalid auth response payload.");
        }

        return session;
    }

    private static JObject ParseJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new WatchSdkApiException("Empty response body from API.");
        }

        try
        {
            return JObject.Parse(text);
        }
        catch (JsonException)
        {
            throw new WatchSdkApiException("Invalid JSON response from API.");
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
        {
            return;
        }

        throw ToApiException(response);
    }

    private static WatchSdkApiException ToApiException(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var json = JObject.Parse(body);
                var title = json["title"]?.ToString();
                var detail = json["detail"]?.ToString();
                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(title)) parts.Add(title);
                if (!string.IsNullOrWhiteSpace(detail)) parts.Add(detail);
                if (parts.Count > 0)
                {
                    return new WatchSdkApiException(string.Join(": ", parts), statusCode);
                }
            }
            catch (JsonException)
            {
                // fall through to raw body
            }

            var trimmed = body.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return new WatchSdkApiException(trimmed, statusCode);
            }
        }

        return new WatchSdkApiException(
            $"Request failed with status {statusCode}.",
            statusCode);
    }

    private static string BuildTimeoutMessage(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();
        var port = uri.IsDefaultPort
            ? (uri.Scheme == "https" ? 443 : 80)
            : uri.Port;

        if (host == "localhost" || host == "127.0.0.1")
        {
            return $"Timed out reaching {uri}. If you are using a physical Android device with a " +
                   $"backend on this computer, run \"adb reverse tcp:{port} tcp:{port}\" and set " +
                   $"apiBaseUrl to \"http://127.0.0.1:{port}\".";
        }

        return $"Timed out reaching {uri}.";
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshLock.Dispose();
    }
}
