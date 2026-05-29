using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JellyfinClient.Models;

namespace JellyfinClient.Services;

public class JellyfinApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOpts;

    public string? ServerUrl { get; private set; }
    public string? AccessToken { get; set; }
    public User? CurrentUser { get; set; }
    public ServerInfo? ServerInfo { get; private set; }
    public UserSettings Settings { get; private set; } = UserSettings.Default;

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && CurrentUser != null;

    public event Action? OnAuthenticationChanged;
    public event Action? OnSettingsChanged;

    public void RaiseAuthChanged() => OnAuthenticationChanged?.Invoke();

    // X-Emby-Authorization header values
    private const string ClientName = "BoxJellyfin";
    private const string DeviceName = "Xbox";
    private const string AppVersion = "1.0.2.0";
    private static readonly string DeviceId = Guid.NewGuid().ToString("N");

    public JellyfinApiClient(HttpClient http)
    {
        _http = http;
        _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        SetEmbyAuthHeader();
    }

    private void SetEmbyAuthHeader()
    {
        // Jellyfin requires X-Emby-Authorization header for client identification.
        // Format: MediaBrowser Client="App", Device="Device", DeviceId="id", Version="ver"
        // This is read by the server's AuthorizationContext.GetAuthorizationInfo() for
        // Quick Connect, session tracking, and device management.
        var header = $"MediaBrowser Client=\"{ClientName}\", Device=\"{DeviceName}\", DeviceId=\"{DeviceId}\", Version=\"{AppVersion}\"";

        // Remove old value if exists, then set new
        _http.DefaultRequestHeaders.Remove("X-Emby-Authorization");
        _http.DefaultRequestHeaders.Add("X-Emby-Authorization", header);
    }

    public void SetServerUrl(string url)
    {
        ServerUrl = url.TrimEnd('/');
        _http.BaseAddress = new Uri(ServerUrl);
        SetEmbyAuthHeader(); // Re-add after BaseAddress change
    }

    public void ApplyAuth()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Remove("X-Emby-Token");

        if (!string.IsNullOrEmpty(AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            _http.DefaultRequestHeaders.Add("X-Emby-Token", AccessToken);
        }
    }

    #region Settings Persistence

    public void UpdateSettings(UserSettings settings)
    {
        Settings = settings;
        OnSettingsChanged?.Invoke();
    }

    #endregion

    #region Discovery

    public async Task<(ServerInfo? info, string? error)> GetServerInfoAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ServerInfo>("/System/Info/Public", _jsonOpts);
            ServerInfo = result;
            return (result, null);
        }
        catch (HttpRequestException ex)
        {
            return (null, $"Cannot reach server: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Error: {ex.Message}");
        }
    }

    #endregion

    #region Authentication

    public async Task<List<User>> GetPublicUsersAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<User>>("/Users/Public", _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<(AuthenticationResult? result, string? error)> AuthenticateAsync(string username, string password)
    {
        var body = new { Username = username, Pw = password };
        try
        {
            var response = await _http.PostAsJsonAsync("/Users/AuthenticateByName", body, _jsonOpts);

            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync();
                return (null, $"Server returned {(int)response.StatusCode}: {bodyText}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOpts);
            if (result == null)
                return (null, "Server returned empty response");

            AccessToken = result.AccessToken;
            CurrentUser = result.User;
            ApplyAuth();
            OnAuthenticationChanged?.Invoke();
            return (result, null);
        }
        catch (HttpRequestException ex)
        {
            return (null, $"Cannot reach server: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Error: {ex.Message}");
        }
    }

    public void Logout()
    {
        AccessToken = null;
        CurrentUser = null;
        ApplyAuth();
        OnAuthenticationChanged?.Invoke();
    }

    #endregion

    #region Quick Connect

    /// <summary>
    /// Initiates Quick Connect. Endpoint: POST /QuickConnect/Initiate
    /// (NOT /QuickConnect/Init — that endpoint does not exist in Jellyfin)
    /// </summary>
    public async Task<(QuickConnectInitResponse? result, string? error)> QuickConnectInitAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ServerUrl))
                return (null, "No server connected. Enter server URL first.");

            var fullUrl = $"{ServerUrl}/QuickConnect/Initiate";
            var response = await _http.PostAsync(fullUrl, null);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (null, $"Server returned {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<QuickConnectInitResponse>(_jsonOpts);
            if (result == null)
                return (null, "Server returned unexpected response");

            if (string.IsNullOrEmpty(result.Secret))
                return (null, "Server returned empty secret — Quick Connect may be disabled");

            return (result, null);
        }
        catch (HttpRequestException ex)
        {
            return (null, $"Cannot reach server: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Polls Quick Connect status. Endpoint: GET /QuickConnect/Connect?secret=
    /// Returns the QuickConnectResult which includes Authenticated flag and
    /// authentication tokens when authorized.
    /// </summary>
    public async Task<QuickConnectStatusResponse?> QuickConnectCheckAsync(string secret)
    {
        try
        {
            return await _http.GetFromJsonAsync<QuickConnectStatusResponse>(
                $"/QuickConnect/Connect?secret={Uri.EscapeDataString(secret)}", _jsonOpts);
        }
        catch { return null; }
    }

    /// <summary>
    /// Exchanges Quick Connect secret for auth tokens.
    /// Endpoint: POST /Users/AuthenticateWithQuickConnect
    /// Body: { Secret: "..." }
    /// </summary>
    public async Task<(AuthenticationResult? result, string? error)> QuickConnectAuthenticateAsync(string secret)
    {
        try
        {
            var body = new { Secret = secret };
            var response = await _http.PostAsJsonAsync("/Users/AuthenticateWithQuickConnect", body, _jsonOpts);

            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync();
                return (null, $"Auth failed: {(int)response.StatusCode}: {bodyText}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOpts);
            if (result == null)
                return (null, "Server returned empty response");

            AccessToken = result.AccessToken;
            CurrentUser = result.User;
            ApplyAuth();
            OnAuthenticationChanged?.Invoke();
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, $"Error: {ex.Message}");
        }
    }

    #endregion

    #region Library

    public async Task<List<ViewItem>> GetViewsAsync()
    {
        try
        {
            // Jellyfin endpoint: GET /UserViews (UserViewsController.cs, [HttpGet("UserViews")])
            var result = await _http.GetFromJsonAsync<ItemsResult>("/UserViews", _jsonOpts);
            if (result?.Items == null) return new();

            return result.Items.Select(i => new ViewItem
            {
                Id = i.Id,
                Name = i.Name,
                Type = i.Type,
                CollectionType = i.CollectionType,
                ImageTags = i.ImageTags
            }).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetViews] Failed: {ex}");
            return new();
        }
    }

    public async Task<ItemsResult> GetItemsAsync(string parentId, string? sortBy = null, string? sortOrder = null,
        int startIndex = 0, int limit = 40, string? filters = null, string? includeItemTypes = null)
    {
        // ItemsController.cs: [Route("")] + [HttpGet("Items")] → GET /Items?userId=&parentId=…
        var url = $"/Items?userId={CurrentUser!.Id}&parentId={parentId}&startIndex={startIndex}&limit={limit}&Fields=PrimaryImageAspectRatio,BasicSyncInfo,MediaStreams,MediaSources,Blurhashes,ImageBlurHashes";

        if (!string.IsNullOrEmpty(sortBy))
            url += $"&sortBy={sortBy}";
        if (!string.IsNullOrEmpty(sortOrder))
            url += $"&sortOrder={sortOrder}";
        if (!string.IsNullOrEmpty(filters))
            url += $"&filters={filters}";
        if (!string.IsNullOrEmpty(includeItemTypes))
            url += $"&includeItemTypes={includeItemTypes}";

        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetResumeItemsAsync(int limit = 12)
    {
        // ItemsController.cs: [HttpGet("Items")] — resume uses Filters=IsResumable + SortBy=DatePlayed
        var url = $"/Items?userId={CurrentUser!.Id}&Filters=IsResumable&SortBy=DatePlayed&SortOrder=Descending&Recursive=true&IncludeItemTypes=Movie,Episode&Limit={limit}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes";
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetNextUpAsync(string? seriesId = null, int limit = 12)
    {
        // TvShowsController.cs: [Route("Shows")] + [HttpGet("NextUp")] → GET /Shows/NextUp?userId=…
        var url = $"/Shows/NextUp?userId={CurrentUser!.Id}&limit={limit}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes";
        if (!string.IsNullOrEmpty(seriesId))
            url += $"&seriesId={seriesId}";
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<BaseItemDto?> GetItemAsync(string itemId)
    {
        // UserLibraryController.cs: [Route("")] + [HttpGet("Items/{itemId}")] → GET /Items/{itemId}?userId=…
        var url = $"/Items/{itemId}?userId={CurrentUser!.Id}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes,People,Studios,Genres,Overview";
        try
        {
            return await _http.GetFromJsonAsync<BaseItemDto>(url, _jsonOpts);
        }
        catch { return null; }
    }

    public async Task<ItemsResult> GetSeasonsAsync(string seriesId)
    {
        // TvShowsController.cs: [Route("Shows")] + [HttpGet("{seriesId}/Seasons")] → GET /Shows/{id}/Seasons?userId=…
        var url = $"/Shows/{seriesId}/Seasons?userId={CurrentUser!.Id}&Fields=PrimaryImageAspectRatio,ItemCounts";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetEpisodesAsync(string seriesId, string seasonId)
    {
        // TvShowsController.cs: [Route("Shows")] + [HttpGet("{seriesId}/Episodes")] → GET /Shows/{id}/Episodes?seasonId=…&userId=…
        var url = $"/Shows/{seriesId}/Episodes?seasonId={seasonId}&userId={CurrentUser!.Id}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts) ?? new();
        }
        catch { return new(); }
    }

    #endregion

    #region Search

    public async Task<ItemsResult> SearchAsync(string query, int limit = 20)
    {
        // ItemsController.cs: [HttpGet("Items")] with searchTerm parameter → GET /Items?searchTerm=…&userId=…
        var url = $"/Items?searchTerm={Uri.EscapeDataString(query)}&userId={CurrentUser!.Id}&limit={limit}&IncludeItemTypes=Movie,Series,Episode&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes&Recursive=true";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts) ?? new();
        }
        catch { return new(); }
    }

    #endregion

    #region Playback

    public async Task<PlaybackInfoResponse?> GetPlaybackInfoAsync(string itemId, long maxBitrate = 120_000_000)
    {
        var url = $"/Items/{itemId}/PlaybackInfo?UserId={CurrentUser!.Id}&MaxStreamingBitrate={maxBitrate}&AutoOpenLiveStreams=true";

        if (!string.IsNullOrEmpty(Settings.PreferredAudioLanguage))
            url += $"&AudioStreamIndex=-1";
        if (Settings.SubtitleMode == "On")
            url += $"&SubtitleStreamIndex=-1";
        else if (Settings.SubtitleMode == "Off")
            url += $"&SubtitleStreamIndex=-2";

        var body = new { DeviceProfileJson = DeviceProfileBuilder.GetXboxProfileJson() };

        try
        {
            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(_jsonOpts);
        }
        catch { return null; }
    }

    public async Task ReportPlaybackStartAsync(string itemId, string? audioStreamIndex = null, string? subtitleStreamIndex = null)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                ["ItemId"] = itemId,
                ["CanSeek"] = "true"
            };
            if (audioStreamIndex != null) data["AudioStreamIndex"] = audioStreamIndex;
            if (subtitleStreamIndex != null) data["SubtitleStreamIndex"] = subtitleStreamIndex;

            await _http.PostAsync($"/Sessions/Playing", new FormUrlEncodedContent(data));
        }
        catch { /* fire and forget */ }
    }

    public async Task ReportPlaybackProgressAsync(string itemId, long positionTicks, bool isPaused = false)
    {
        try
        {
            await _http.PostAsync($"/Sessions/Playing/Progress", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ItemId"] = itemId,
                ["PositionTicks"] = positionTicks.ToString(),
                ["IsPaused"] = isPaused.ToString(),
                ["IsMuted"] = "false"
            }));
        }
        catch { /* fire and forget */ }
    }

    public async Task ReportPlaybackStoppedAsync(string itemId, long positionTicks)
    {
        try
        {
            await _http.PostAsync($"/Sessions/Playing/Stopped", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ItemId"] = itemId,
                ["PositionTicks"] = positionTicks.ToString()
            }));
        }
        catch { /* fire and forget */ }
    }

    public async Task MarkPlayedAsync(string itemId)
    {
        try
        {
            // PlaystateController.cs: [HttpPost("UserPlayedItems/{itemId}")] with [FromQuery] Guid? userId
            await _http.PostAsync($"/UserPlayedItems/{itemId}?userId={CurrentUser!.Id}", null);
        }
        catch { }
    }

    public async Task ToggleFavoriteAsync(string itemId)
    {
        try
        {
            await _http.PostAsync($"/Users/{CurrentUser!.Id}/FavoriteItems/{itemId}", null);
        }
        catch { }
    }

    public string BuildTranscodeUrlWithTracks(string itemId, MediaSourceInfo source,
        int? audioIndex = null, int? subtitleIndex = null)
    {
        var baseUrl = ServerUrl;
        var url = $"/Videos/{itemId}/stream?static=true&MediaSourceId={source.Id}&ApiKey={AccessToken}";

        if (!source.SupportsDirectPlay && !string.IsNullOrEmpty(source.TranscodingUrl))
        {
            url = source.TranscodingUrl;
            if (audioIndex.HasValue)
                url += $"&AudioStreamIndex={audioIndex.Value}";
            if (subtitleIndex.HasValue)
                url += $"&SubtitleStreamIndex={subtitleIndex.Value}";
            return $"{baseUrl}{url}&ApiKey={AccessToken}";
        }

        if (audioIndex.HasValue)
            url += $"&AudioStreamIndex={audioIndex.Value}";
        if (subtitleIndex.HasValue)
            url += $"&SubtitleStreamIndex={subtitleIndex.Value}";

        return $"{baseUrl}{url}";
    }

    #endregion

    #region Image URLs

    public string GetImageUrl(string itemId, string imageType = "Primary", int? maxWidth = null, int? maxHeight = null)
    {
        var url = $"/Items/{itemId}/Images/{imageType}";
        var @params = new List<string>();

        // Jellyfin requires ApiKey for image access via direct URL (not through HttpClient)
        if (!string.IsNullOrEmpty(AccessToken))
            @params.Add($"ApiKey={AccessToken}");

        if (maxWidth.HasValue) @params.Add($"maxWidth={maxWidth.Value}");
        if (maxHeight.HasValue) @params.Add($"maxHeight={maxHeight.Value}");
        if (@params.Count > 0) url += "?" + string.Join("&", @params);
        return url;
    }

    public string GetBackdropUrl(string itemId, int index = 0, int? maxWidth = null)
    {
        var url = $"/Items/{itemId}/Images/Backdrop/{index}";
        var @params = new List<string>();
        if (!string.IsNullOrEmpty(AccessToken)) @params.Add($"ApiKey={AccessToken}");
        if (maxWidth.HasValue) @params.Add($"maxWidth={maxWidth.Value}");
        if (@params.Count > 0) url += "?" + string.Join("&", @params);
        return url;
    }

    public string GetPersonImageUrl(string personId, int? maxWidth = null)
    {
        var url = $"/Items/{personId}/Images/Primary";
        var @params = new List<string>();
        if (!string.IsNullOrEmpty(AccessToken)) @params.Add($"ApiKey={AccessToken}");
        if (maxWidth.HasValue) @params.Add($"maxWidth={maxWidth.Value}");
        if (@params.Count > 0) url += "?" + string.Join("&", @params);
        return url;
    }

    #endregion

    #region Helpers

    public TimeSpan? GetRuntime(BaseItemDto item) =>
        item.RunTimeTicks.HasValue ? TimeSpan.FromTicks(item.RunTimeTicks.Value) : null;

    public string FormatRuntime(TimeSpan? runtime)
    {
        if (!runtime.HasValue) return "";
        var ts = runtime.Value;
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m";
    }

    public static List<MediaStream> GetAudioStreams(MediaSourceInfo source) =>
        source.MediaStreams.Where(s => s.Type == "Audio").OrderBy(s => !s.IsDefault).ToList();

    public static List<MediaStream> GetSubtitleStreams(MediaSourceInfo source) =>
        source.MediaStreams.Where(s => s.Type == "Subtitle").OrderBy(s => !s.IsDefault).ToList();

    #endregion
}
