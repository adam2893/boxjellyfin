using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JellyfinClient.Models;

namespace JellyfinClient.Services;

/// <summary>
/// Typed Jellyfin API client — all endpoints verified against Jellyfin 10.10+ source.
/// Generated from the Jellyfin OpenAPI spec pattern (same source as Kotlin SDK).
/// </summary>
public interface IJellyfinClient
{
    // ── Connection ──────────────────────────────────────────────
    string? ServerUrl { get; }
    string? AccessToken { get; }
    User? CurrentUser { get; }
    ServerInfo? ServerInfo { get; }
    bool IsAuthenticated { get; }

    void SetServerUrl(string url);
    void SetAccessToken(string? token);

    // ── Discovery ───────────────────────────────────────────────
    Task<(ServerInfo? info, string? error)> GetServerInfoAsync(CancellationToken ct = default);

    // ── Authentication ──────────────────────────────────────────
    Task<List<User>> GetPublicUsersAsync(CancellationToken ct = default);
    Task<(AuthenticationResult? result, string? error)> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    void Logout();

    // ── Quick Connect ───────────────────────────────────────────
    Task<(QuickConnectInitResponse? result, string? error)> QuickConnectInitAsync(CancellationToken ct = default);
    Task<QuickConnectStatusResponse?> QuickConnectCheckAsync(string secret, CancellationToken ct = default);
    Task<(AuthenticationResult? result, string? error)> QuickConnectAuthenticateAsync(string secret, CancellationToken ct = default);

    // ── Library ─────────────────────────────────────────────────
    Task<List<ViewItem>> GetViewsAsync(CancellationToken ct = default);
    Task<ItemsResult> GetItemsAsync(string parentId, string? sortBy = null, string? sortOrder = null,
        int startIndex = 0, int limit = 40, string? filters = null, string? includeItemTypes = null,
        CancellationToken ct = default);
    Task<ItemsResult> GetResumeItemsAsync(int limit = 12, CancellationToken ct = default);
    Task<ItemsResult> GetNextUpAsync(string? seriesId = null, int limit = 12, CancellationToken ct = default);
    Task<BaseItemDto?> GetItemAsync(string itemId, CancellationToken ct = default);
    Task<ItemsResult> GetSeasonsAsync(string seriesId, CancellationToken ct = default);
    Task<ItemsResult> GetEpisodesAsync(string seriesId, string seasonId, CancellationToken ct = default);

    // ── Search ──────────────────────────────────────────────────
    Task<ItemsResult> SearchAsync(string query, int limit = 20, CancellationToken ct = default);

    // ── Playback ────────────────────────────────────────────────
    Task<PlaybackInfoResponse?> GetPlaybackInfoAsync(string itemId, long maxBitrate = 120_000_000, CancellationToken ct = default);
    Task ReportPlaybackStartAsync(string itemId, string? audioStreamIndex = null, string? subtitleStreamIndex = null);
    Task ReportPlaybackProgressAsync(string itemId, long positionTicks, bool isPaused = false);
    Task ReportPlaybackStoppedAsync(string itemId, long positionTicks);
    Task MarkPlayedAsync(string itemId);
    Task ToggleFavoriteAsync(string itemId);
    string BuildTranscodeUrlWithTracks(string itemId, MediaSourceInfo source, int? audioIndex = null, int? subtitleIndex = null);

    // ── Images ──────────────────────────────────────────────────
    string GetImageUrl(string itemId, string imageType = "Primary", int? maxWidth = null, int? maxHeight = null, string? tag = null);
    string GetBackdropUrl(string itemId, int index = 0, int? maxWidth = null, string? tag = null);
    string GetPersonImageUrl(string personId, int? maxWidth = null);

    // ── Helpers ─────────────────────────────────────────────────
    TimeSpan? GetRuntime(BaseItemDto item);
    string FormatRuntime(TimeSpan? runtime);
    List<MediaStream> GetAudioStreams(MediaSourceInfo source);
    List<MediaStream> GetSubtitleStreams(MediaSourceInfo source);

    // ── Session ────────────────────────────────────────────────
    Task<bool> ValidateTokenAsync();

    // ── Events ──────────────────────────────────────────────────
    event Action? OnAuthenticationChanged;
    void RaiseAuthChanged();
}
