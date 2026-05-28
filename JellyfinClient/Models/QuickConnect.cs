using System.Text.Json;
using System.Text.Json.Serialization;
using JellyfinClient.Models;

namespace JellyfinClient.Models;

public class QuickConnectRequest
{
    [JsonPropertyName("Secret")]
    public string Secret { get; set; } = string.Empty;
}

public class QuickConnectInitResponse
{
    [JsonPropertyName("Secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;
}

public class QuickConnectAuthorizeResponse
{
    [JsonPropertyName("Authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("UserId")]
    public string? UserId { get; set; }

    [JsonPropertyName("UserName")]
    public string? UserName { get; set; }

    [JsonPropertyName("Application")]
    public string? Application { get; set; }
}

public class QuickConnectConnectResponse
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
    /// <summary>Max streaming bitrate in bps. 0 = unlimited.</summary>
    public long MaxStreamingBitrate { get; set; } = 120_000_000; // 120 Mbps

    /// <summary>Preferred audio language (e.g., "eng", "jpn"). Null = default.</summary>
    public string? PreferredAudioLanguage { get; set; }

    /// <summary>Preferred subtitle language. Null = default.</summary>
    public string? PreferredSubtitleLanguage { get; set; }

    /// <summary>Default subtitle mode: On, Off, OnlyForced, Default.</summary>
    public string SubtitleMode { get; set; } = "Default";

    /// <summary>Auto-play next episode in series.</summary>
    public bool AutoPlayNextEpisode { get; set; } = true;

    /// <summary>Preferred audio codec priority (comma-separated).</summary>
    public string PreferredAudioCodecs { get; set; } = "opus,aac,flac,truehd,dts";

    /// <summary>Preferred subtitle format.</summary>
    public string PreferredSubtitleFormat { get; set; } = "srt";

    /// <summary>Whether to prefer subtitles with foreign audio only.</summary>
    public bool PreferForeignSubtitles { get; set; } = true;

    /// <summary>Remember last playback position.</summary>
    public bool RememberPlaybackPosition { get; set; } = true;

    /// <summary>Subtitle appearance: font size.</summary>
    public int SubtitleFontSize { get; set; } = 100; // percentage

    /// <summary>Max concurrent streams.</summary>
    public int MaxConcurrentStreams { get; set; } = 3;

    public static readonly UserSettings Default = new();

    public string GetMaxBitrateLabel()
    {
        if (MaxStreamingBitrate <= 0) return "Maximum";
        if (MaxStreamingBitrate >= 100_000_000) return "1080p+ (120 Mbps)";
        if (MaxStreamingBitrate >= 50_000_000) return "1080p (50 Mbps)";
        if (MaxStreamingBitrate >= 30_000_000) return "1080p (30 Mbps)";
        if (MaxStreamingBitrate >= 15_000_000) return "720p (15 Mbps)";
        if (MaxStreamingBitrate >= 8_000_000) return "720p (8 Mbps)";
        if (MaxStreamingBitrate >= 4_000_000) return "480p (4 Mbps)";
        if (MaxStreamingBitrate >= 2_000_000) return "360p (2 Mbps)";
        if (MaxStreamingBitrate >= 1_000_000) return "240p (1 Mbps)";
        return "Auto";
    }

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
