using System.Text.Json.Serialization;

namespace JellyfinClient.Models;

public class QuickConnectInitResponse
{
    [JsonPropertyName("Secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;
}

public class QuickConnectStatusResponse
{
    [JsonPropertyName("Authenticated")]
    public bool Authenticated { get; set; }

    [JsonPropertyName("Authentication")]
    public QuickConnectAuthResult? Authentication { get; set; }
}

public class QuickConnectAuthResult
{
    [JsonPropertyName("User")]
    public User User { get; set; } = new();

    [JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("ServerId")]
    public string ServerId { get; set; } = string.Empty;
}

public class UserSettings
{
    public long MaxStreamingBitrate { get; set; } = 120_000_000;
    public string? PreferredAudioLanguage { get; set; }
    public string? PreferredSubtitleLanguage { get; set; }
    public string SubtitleMode { get; set; } = "Default";
    public bool AutoPlayNextEpisode { get; set; } = true;
    public string PreferredAudioCodecs { get; set; } = "opus,aac,flac,truehd,dts";
    public string PreferredSubtitleFormat { get; set; } = "srt";
    public bool PreferForeignSubtitles { get; set; } = true;
    public bool RememberPlaybackPosition { get; set; } = true;
    public int SubtitleFontSize { get; set; } = 100;
    public int MaxConcurrentStreams { get; set; } = 3;

    public static readonly UserSettings Default = new();

    public static readonly (string Label, long Value)[] BitratePresets =
    {
        ("Maximum (120 Mbps)", 120_000_000),
        ("1080p High (50 Mbps)", 50_000_000),
        ("1080p (30 Mbps)", 30_000_000),
        ("720p (15 Mbps)", 15_000_000),
        ("720p Low (8 Mbps)", 8_000_000),
        ("480p (4 Mbps)", 4_000_000),
        ("360p (2 Mbps)", 2_000_000),
    };
}
