using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JellyfinClient.Models;

namespace JellyfinClient.Services;

/// <summary>
/// Jellyfin API client — all 23+ endpoints verified against Jellyfin 10.10+ source.
/// Implements IJellyfinClient for testability and dependency injection.
/// </summary>
public class JellyfinApiClient : IJellyfinClient
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

    public void SetAccessToken(string? token)
    {
        AccessToken = token;
        ApplyAuth();
    }

    // ═══════════════════════════════════════════════════════════
    // Session persistence (Wholphin-style restore)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Validates whether the current access token is still accepted by the server
    /// by fetching public system info. Returns true if the token is valid.
    /// </summary>
    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ServerUrl) || string.IsNullOrEmpty(AccessToken))
                return false;

            var (info, _) = await GetServerInfoAsync();
            if (info == null) return false;

            ServerInfo = info;
            var views = await GetViewsAsync();
            return views.Count > 0;
        }
        catch { return false; }
    }

    // X-Emby-Authorization header values
    private const string ClientName = "BoxJellyfin";
    private const string DeviceName = "Xbox";
    private const string AppVersion = "1.0.6.30";
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
        var header = $"MediaBrowser Client=\"{ClientName}\", Device=\"{DeviceName}\", DeviceId=\"{DeviceId}\", Version=\"{AppVersion}\"";
        _http.DefaultRequestHeaders.Remove("X-Emby-Authorization");
        _http.DefaultRequestHeaders.Add("X-Emby-Authorization", header);
    }

    public void SetServerUrl(string url)
    {
        ServerUrl = url.TrimEnd('/');
        _http.BaseAddress = new Uri(ServerUrl);
        SetEmbyAuthHeader();
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

    public void UpdateSettings(UserSettings settings)
    {
        Settings = settings;
        OnSettingsChanged?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════
    // Discovery
    // ═══════════════════════════════════════════════════════════════

    public async Task<(ServerInfo? info, string? error)> GetServerInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ServerInfo>("/System/Info/Public", _jsonOpts, ct);
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

    // ═══════════════════════════════════════════════════════════════
    // Authentication
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<User>> GetPublicUsersAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<User>>("/Users/Public", _jsonOpts, ct);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<User>("/Users/Me", _jsonOpts, ct);
            return result;
        }
        catch { return null; }
    }

    public async Task<(AuthenticationResult? result, string? error)> AuthenticateAsync(
        string username, string password, CancellationToken ct = default)
    {
        var body = new { Username = username, Pw = password };
        try
        {
            var response = await _http.PostAsJsonAsync("/Users/AuthenticateByName", body, _jsonOpts, ct);

            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync();
                return (null, $"Server returned {(int)response.StatusCode}: {bodyText}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOpts, ct);
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

    // ═══════════════════════════════════════════════════════════════
    // Quick Connect
    // ═══════════════════════════════════════════════════════════════

    public async Task<(QuickConnectInitResponse? result, string? error)> QuickConnectInitAsync(CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(ServerUrl))
                return (null, "No server connected. Enter server URL first.");

            var fullUrl = $"{ServerUrl}/QuickConnect/Initiate";
            var response = await _http.PostAsync(fullUrl, null, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (null, $"Server returned {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<QuickConnectInitResponse>(_jsonOpts, ct);
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

    public async Task<QuickConnectStatusResponse?> QuickConnectCheckAsync(string secret, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<QuickConnectStatusResponse>(
                $"/QuickConnect/Connect?secret={Uri.EscapeDataString(secret)}", _jsonOpts, ct);
        }
        catch { return null; }
    }

    public async Task<(AuthenticationResult? result, string? error)> QuickConnectAuthenticateAsync(
        string secret, CancellationToken ct = default)
    {
        try
        {
            var body = new { Secret = secret };
            var response = await _http.PostAsJsonAsync("/Users/AuthenticateWithQuickConnect", body, _jsonOpts, ct);

            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync();
                return (null, $"Auth failed: {(int)response.StatusCode}: {bodyText}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOpts, ct);
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

    // ═══════════════════════════════════════════════════════════════
    // Library
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<ViewItem>> GetViewsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>("/UserViews", _jsonOpts, ct);
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

    public async Task<ItemsResult> GetItemsAsync(string parentId,
        string? sortBy = null, string? sortOrder = null,
        int startIndex = 0, int limit = 40, string? filters = null,
        string? includeItemTypes = null, CancellationToken ct = default)
    {
        var url = $"/Items?userId={CurrentUser!.Id}&parentId={parentId}&startIndex={startIndex}&limit={limit}" +
            "&fields=PrimaryImageAspectRatio,BasicSyncInfo,MediaSources,ImageTags&enableImages=true&enableImageTypes=Primary,Thumb,Backdrop&imageTypeLimit=3";

        if (!string.IsNullOrEmpty(sortBy)) url += $"&sortBy={sortBy}";
        if (!string.IsNullOrEmpty(sortOrder)) url += $"&sortOrder={sortOrder}";
        if (!string.IsNullOrEmpty(filters)) url += $"&filters={filters}";
        if (!string.IsNullOrEmpty(includeItemTypes)) url += $"&includeItemTypes={includeItemTypes}";

        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts, ct);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetResumeItemsAsync(int limit = 12, CancellationToken ct = default)
    {
        var url = $"/Items?userId={CurrentUser!.Id}&Filters=IsResumable&SortBy=DatePlayed&SortOrder=Descending" +
            $"&Recursive=true&IncludeItemTypes=Movie,Episode&Limit={limit}" +
            "&fields=PrimaryImageAspectRatio,MediaSources,ImageTags&enableImages=true&enableImageTypes=Primary,Thumb,Backdrop&imageTypeLimit=3";
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts, ct);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetNextUpAsync(string? seriesId = null, int limit = 12, CancellationToken ct = default)
    {
        var url = $"/Shows/NextUp?userId={CurrentUser!.Id}&limit={limit}" +
            "&fields=PrimaryImageAspectRatio,MediaSources,ImageTags&enableImages=true&enableImageTypes=Primary,Thumb,Backdrop&imageTypeLimit=3";
        if (!string.IsNullOrEmpty(seriesId)) url += $"&seriesId={seriesId}";
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts, ct);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<BaseItemDto?> GetItemAsync(string itemId, CancellationToken ct = default)
    {
        var url = $"/Items/{itemId}?userId={CurrentUser!.Id}" +
            "&fields=PrimaryImageAspectRatio,MediaSources,ImageTags,People,Studios,Genres,Overview,MediaStreams&enableImages=true";
        try
        {
            return await _http.GetFromJsonAsync<BaseItemDto>(url, _jsonOpts, ct);
        }
        catch { return null; }
    }

    public async Task<ItemsResult> GetSeasonsAsync(string seriesId, CancellationToken ct = default)
    {
        var url = $"/Shows/{seriesId}/Seasons?userId={CurrentUser!.Id}" +
            "&fields=PrimaryImageAspectRatio,ItemCounts,ImageTags&enableImages=true&enableImageTypes=Primary&imageTypeLimit=3";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts, ct) ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetEpisodesAsync(string seriesId, string seasonId, CancellationToken ct = default)
    {
        var url = $"/Shows/{seriesId}/Episodes?seasonId={seasonId}&userId={CurrentUser!.Id}" +
            "&fields=PrimaryImageAspectRatio,MediaSources,ImageTags&enableImages=true&enableImageTypes=Primary,Thumb&imageTypeLimit=3";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts, ct) ?? new();
        }
        catch { return new(); }
    }

    // ═══════════════════════════════════════════════════════════════
    // Search
    // ═══════════════════════════════════════════════════════════════

    public async Task<ItemsResult> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var url = $"/Items?searchTerm={Uri.EscapeDataString(query)}&userId={CurrentUser!.Id}&limit={limit}" +
            "&IncludeItemTypes=Movie,Series,Episode" +
            "&fields=PrimaryImageAspectRatio,MediaSources,ImageTags&enableImages=true&enableImageTypes=Primary,Thumb&imageTypeLimit=3&Recursive=true";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts, ct) ?? new();
        }
        catch { return new(); }
    }

    // ═══════════════════════════════════════════════════════════════
    // Playback
    // ═══════════════════════════════════════════════════════════════

    public async Task<PlaybackInfoResponse?> GetPlaybackInfoAsync(string itemId,
        long maxBitrate = 120_000_000, CancellationToken ct = default)
    {
        var url = $"/Items/{itemId}/PlaybackInfo?UserId={CurrentUser!.Id}" +
            $"&MaxStreamingBitrate={maxBitrate}&AutoOpenLiveStreams=true";

        if (!string.IsNullOrEmpty(Settings.PreferredAudioLanguage)) url += "&AudioStreamIndex=-1";
        if (Settings.SubtitleMode == "On") url += "&SubtitleStreamIndex=-1";
        else if (Settings.SubtitleMode == "Off") url += "&SubtitleStreamIndex=-2";

        var body = new { DeviceProfileJson = DeviceProfileBuilder.GetXboxProfileJson() };

        try
        {
            var response = await _http.PostAsJsonAsync(url, body, _jsonOpts, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(_jsonOpts, ct);
        }
        catch { return null; }
    }

    public async Task ReportPlaybackStartAsync(string itemId,
        string? audioStreamIndex = null, string? subtitleStreamIndex = null)
    {
        try
        {
            var data = new Dictionary<string, string> { ["ItemId"] = itemId, ["CanSeek"] = "true" };
            if (audioStreamIndex != null) data["AudioStreamIndex"] = audioStreamIndex;
            if (subtitleStreamIndex != null) data["SubtitleStreamIndex"] = subtitleStreamIndex;
            await _http.PostAsync("/Sessions/Playing", new FormUrlEncodedContent(data));
        }
        catch { /* fire and forget */ }
    }

    public async Task ReportPlaybackProgressAsync(string itemId, long positionTicks, bool isPaused = false)
    {
        try
        {
            await _http.PostAsync("/Sessions/Playing/Progress", new FormUrlEncodedContent(new Dictionary<string, string>
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
            await _http.PostAsync("/Sessions/Playing/Stopped", new FormUrlEncodedContent(new Dictionary<string, string>
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
        var url = $"/Videos/{itemId}/stream?MediaSourceId={source.Id}&ApiKey={AccessToken}";

        if (audioIndex.HasValue) url += $"&AudioStreamIndex={audioIndex.Value}";
        if (subtitleIndex.HasValue) url += $"&SubtitleStreamIndex={subtitleIndex.Value}";
        return $"{baseUrl}{url}";
    }

    // ═══════════════════════════════════════════════════════════════
    // Images
    // ═══════════════════════════════════════════════════════════════

    public string GetImageUrl(string itemId, string imageType = "Primary",
        int? maxWidth = null, int? maxHeight = null, string? tag = null)
    {
        var url = $"/Items/{itemId}/Images/{imageType}";
        var @params = new List<string>();
        if (!string.IsNullOrEmpty(AccessToken)) @params.Add($"ApiKey={AccessToken}");
        if (!string.IsNullOrEmpty(tag)) @params.Add($"tag={tag}");
        if (maxWidth.HasValue) @params.Add($"maxWidth={maxWidth.Value}");
        if (maxHeight.HasValue) @params.Add($"maxHeight={maxHeight.Value}");
        @params.Add("quality=90");
        @params.Add("format=Jpeg");
        if (@params.Count > 0) url += "?" + string.Join("&", @params);
        return url;
    }

    public string GetBackdropUrl(string itemId, int index = 0, int? maxWidth = null, string? tag = null)
    {
        var url = $"/Items/{itemId}/Images/Backdrop/{index}";
        var @params = new List<string>();
        if (!string.IsNullOrEmpty(AccessToken)) @params.Add($"ApiKey={AccessToken}");
        if (!string.IsNullOrEmpty(tag)) @params.Add($"tag={tag}");
        if (maxWidth.HasValue) @params.Add($"maxWidth={maxWidth.Value}");
        @params.Add("quality=90");
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

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    public TimeSpan? GetRuntime(BaseItemDto item) =>
        item.RunTimeTicks.HasValue ? TimeSpan.FromTicks(item.RunTimeTicks.Value) : null;

    public string FormatRuntime(TimeSpan? runtime)
    {
        if (!runtime.HasValue) return "";
        var ts = runtime.Value;
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m";
    }

    public List<MediaStream> GetAudioStreams(MediaSourceInfo source) =>
        source.MediaStreams.Where(s => s.Type == "Audio").OrderBy(s => !s.IsDefault).ToList();

    public List<MediaStream> GetSubtitleStreams(MediaSourceInfo source) =>
        source.MediaStreams.Where(s => s.Type == "Subtitle").OrderBy(s => !s.IsDefault).ToList();
}


