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
    public string? AccessToken { get; private set; }
    public User? CurrentUser { get; private set; }
    public ServerInfo? ServerInfo { get; private set; }
    public UserSettings Settings { get; private set; } = UserSettings.Default;

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && CurrentUser != null;

    public event Action? OnAuthenticationChanged;
    public event Action? OnSettingsChanged;

    public JellyfinApiClient(HttpClient http)
    {
        _http = http;
        _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public void SetServerUrl(string url)
    {
        ServerUrl = url.TrimEnd('/');
        _http.BaseAddress = new Uri(ServerUrl);
    }

    private void ApplyAuth()
    {
        if (!string.IsNullOrEmpty(AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            _http.DefaultRequestHeaders.Add("X-Emby-Token", AccessToken);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
            _http.DefaultRequestHeaders.Remove("X-Emby-Token");
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

    public async Task<ServerInfo?> GetServerInfoAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ServerInfo>("/System/Info/Public", _jsonOpts);
            ServerInfo = result;
            return result;
        }
        catch { return null; }
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

    public async Task<AuthenticationResult?> AuthenticateAsync(string username, string password)
    {
        var body = new { Username = username, Pw = password };
        try
        {
            var response = await _http.PostAsJsonAsync("/Users/AuthenticateByName", body);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOpts);
            if (result != null)
            {
                AccessToken = result.AccessToken;
                CurrentUser = result.User;
                ApplyAuth();
                OnAuthenticationChanged?.Invoke();
            }
            return result;
        }
        catch (HttpRequestException) { return null; }
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

    public async Task<QuickConnectInitResponse?> QuickConnectInitAsync()
    {
        try
        {
            var response = await _http.PostAsync("/QuickConnect/Init", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<QuickConnectInitResponse>(_jsonOpts);
        }
        catch { return null; }
    }

    public async Task<QuickConnectAuthorizeResponse?> QuickConnectCheckAuthAsync(string secret)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<QuickConnectAuthorizeResponse>(
                $"/QuickConnect/Authorize?secret={secret}", _jsonOpts);
            return result;
        }
        catch { return null; }
    }

    public async Task<AuthenticationResult?> QuickConnectConnectAsync(string secret)
    {
        try
        {
            var response = await _http.PostAsync($"/QuickConnect/Connect?secret={secret}", null);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOpts);
            if (result != null)
            {
                AccessToken = result.AccessToken;
                CurrentUser = result.User;
                ApplyAuth();
                OnAuthenticationChanged?.Invoke();
            }
            return result;
        }
        catch { return null; }
    }

    #endregion

    #region Library

    public async Task<List<ViewItem>> GetViewsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>("/Users/Items/Views", _jsonOpts);
            if (result?.Items == null) return new();

            return result.Items.Select(i => new ViewItem
            {
                Id = i.Id,
                Name = i.Name,
                Type = i.Type,
                ImageTags = i.ImageTags
            }).ToList();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetItemsAsync(string parentId, string? sortBy = null, string? sortOrder = null,
        int startIndex = 0, int limit = 40, string? filters = null, string? includeItemTypes = null)
    {
        var url = $"/Users/{CurrentUser!.Id}/Items?ParentId={parentId}&StartIndex={startIndex}&Limit={limit}&Fields=PrimaryImageAspectRatio,BasicSyncInfo,MediaStreams,MediaSources,Blurhashes,ImageBlurHashes";

        if (!string.IsNullOrEmpty(sortBy))
            url += $"&SortBy={sortBy}";
        if (!string.IsNullOrEmpty(sortOrder))
            url += $"&SortOrder={sortOrder}";
        if (!string.IsNullOrEmpty(filters))
            url += $"&Filters={filters}";
        if (!string.IsNullOrEmpty(includeItemTypes))
            url += $"&IncludeItemTypes={includeItemTypes}";

        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetResumeItemsAsync(int limit = 12)
    {
        var url = $"/Users/{CurrentUser!.Id}/Items/Resume?Limit={limit}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes&MediaTypes=Video";
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetNextUpAsync(string? seriesId = null, int limit = 12)
    {
        var url = $"/Shows/NextUp?UserId={CurrentUser!.Id}&Limit={limit}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes";
        if (!string.IsNullOrEmpty(seriesId))
            url += $"&SeriesId={seriesId}";
        try
        {
            var result = await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts);
            return result ?? new();
        }
        catch { return new(); }
    }

    public async Task<BaseItemDto?> GetItemAsync(string itemId)
    {
        var url = $"/Users/{CurrentUser!.Id}/Items/{itemId}?Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes,People,Studios,Genres";
        try
        {
            return await _http.GetFromJsonAsync<BaseItemDto>(url, _jsonOpts);
        }
        catch { return null; }
    }

    public async Task<ItemsResult> GetSeasonsAsync(string seriesId)
    {
        var url = $"/Shows/{seriesId}/Seasons?UserId={CurrentUser!.Id}&Fields=PrimaryImageAspectRatio,ItemCounts";
        try
        {
            return await _http.GetFromJsonAsync<ItemsResult>(url, _jsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task<ItemsResult> GetEpisodesAsync(string seriesId, string seasonId)
    {
        var url = $"/Shows/{seriesId}/Episodes?SeasonId={seasonId}&UserId={CurrentUser!.Id}&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes";
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
        var url = $"/Users/{CurrentUser!.Id}/Items?SearchTerm={Uri.EscapeDataString(query)}&Limit={limit}&IncludeItemTypes=Movie,Series,Episode&Fields=PrimaryImageAspectRatio,MediaSources,MediaStreams,Blurhashes,ImageBlurHashes";
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

        // Include audio/subtitle preferences from settings
        if (!string.IsNullOrEmpty(Settings.PreferredAudioLanguage))
            url += $"&AudioStreamIndex=-1"; // Let server pick by language preference
        if (Settings.SubtitleMode == "On")
            url += $"&SubtitleStreamIndex=-1"; // Enable default subtitle
        else if (Settings.SubtitleMode == "Off")
            url += $"&SubtitleStreamIndex=-2"; // Disable subtitles

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
            await _http.DeleteAsync($"/Users/{CurrentUser!.Id}/PlayedItems/{itemId}");
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

    /// <summary>
    /// Build a transcoding URL with specific audio/subtitle track selection.
    /// </summary>
    public string BuildTranscodeUrlWithTracks(string itemId, MediaSourceInfo source,
        int? audioIndex = null, int? subtitleIndex = null)
    {
        var baseUrl = ServerUrl;
        var url = $"/Videos/{itemId}/stream?static=true&MediaSourceId={source.Id}&ApiKey={AccessToken}";

        // If we need to switch tracks on a transcode, we need a full transcode URL
        if (!source.SupportsDirectPlay && !string.IsNullOrEmpty(source.TranscodingUrl))
        {
            url = source.TranscodingUrl;
            if (audioIndex.HasValue)
                url += $"&AudioStreamIndex={audioIndex.Value}";
            if (subtitleIndex.HasValue)
                url += $"&SubtitleStreamIndex={subtitleIndex.Value}";
            return $"{baseUrl}{url}&ApiKey={AccessToken}";
        }

        // For direct play / direct stream, track changes may need transcoding
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
        if (maxWidth.HasValue) @params.Add($"maxWidth={maxWidth.Value}");
        if (maxHeight.HasValue) @params.Add($"maxHeight={maxHeight.Value}");
        if (@params.Count > 0) url += "?" + string.Join("&", @params);
        return url;
    }

    public string GetBackdropUrl(string itemId, int index = 0, int? maxWidth = null)
    {
        var url = $"/Items/{itemId}/Images/Backdrop/{index}";
        if (maxWidth.HasValue) url += $"?maxWidth={maxWidth.Value}";
        return url;
    }

    public string GetPersonImageUrl(string personId, int? maxWidth = null)
    {
        var url = $"/Items/{personId}/Images/Primary";
        if (maxWidth.HasValue) url += $"?maxWidth={maxWidth.Value}";
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
