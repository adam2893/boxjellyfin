using System.Text.Json.Serialization;

namespace JellyfinClient.Models;

public class ServerInfo
{
    [JsonPropertyName("ServerName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("ProductVersion")]
    public string ProductVersion { get; set; } = string.Empty;
}

public class User
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ServerId")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("HasPassword")]
    public bool HasPassword { get; set; }

    [JsonPropertyName("PrimaryImageTag")]
    public string? PrimaryImageTag { get; set; }
}

public class AuthenticationResult
{
    [JsonPropertyName("User")]
    public User User { get; set; } = new();

    [JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("ServerId")]
    public string ServerId { get; set; } = string.Empty;
}

public class BaseItemDto
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("SortName")]
    public string SortName { get; set; } = string.Empty;

    [JsonPropertyName("Overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("ProductionYear")]
    public int? ProductionYear { get; set; }

    [JsonPropertyName("PremiereDate")]
    public DateTime? PremiereDate { get; set; }

    [JsonPropertyName("RunTimeTicks")]
    public long? RunTimeTicks { get; set; }

    [JsonPropertyName("CommunityRating")]
    public float? CommunityRating { get; set; }

    [JsonPropertyName("OfficialRating")]
    public string? OfficialRating { get; set; }

    [JsonPropertyName("Genres")]
    public List<string> Genres { get; set; } = new();

    [JsonPropertyName("Studios")]
    public List<NameIdPair> Studios { get; set; } = new();

    [JsonPropertyName("People")]
    public List<BaseItemPerson> People { get; set; } = new();

    [JsonPropertyName("ImageTags")]
    public Dictionary<string, string> ImageTags { get; set; } = new();

    [JsonPropertyName("BackdropImageTags")]
    public List<string> BackdropImageTags { get; set; } = new();

    [JsonPropertyName("ParentBackdropImageTags")]
    public List<string> ParentBackdropImageTags { get; set; } = new();

    [JsonPropertyName("ParentBackdropItemId")]
    public string? ParentBackdropItemId { get; set; }

    [JsonPropertyName("SeriesId")]
    public string? SeriesId { get; set; }

    [JsonPropertyName("SeriesName")]
    public string? SeriesName { get; set; }

    [JsonPropertyName("SeasonId")]
    public string? SeasonId { get; set; }

    [JsonPropertyName("SeasonName")]
    public string? SeasonName { get; set; }

    [JsonPropertyName("IndexNumber")]
    public int? IndexNumber { get; set; }

    [JsonPropertyName("ParentIndexNumber")]
    public int? ParentIndexNumber { get; set; }

    [JsonPropertyName("ChildCount")]
    public int? ChildCount { get; set; }

    [JsonPropertyName("RecursiveItemCount")]
    public int? RecursiveItemCount { get; set; }

    [JsonPropertyName("UserData")]
    public UserItemDataDto UserData { get; set; } = new();

    [JsonPropertyName("MediaStreams")]
    public List<MediaStream> MediaStreams { get; set; } = new();

    [JsonPropertyName("MediaSources")]
    public List<MediaSourceInfo> MediaSources { get; set; } = new();

    [JsonPropertyName("BackdropBlurhash")]
    public Dictionary<string, string>? BackdropBlurhash { get; set; }

    [JsonPropertyName("ImageBlurHashes")]
    public Dictionary<string, Dictionary<string, string>>? ImageBlurHashes { get; set; }

    [JsonPropertyName("ParentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("CollectionType")]
    public string? CollectionType { get; set; }

    public string? PrimaryImageBlurHash
    {
        get
        {
            if (ImageBlurHashes != null && ImageBlurHashes.TryGetValue("Primary", out var tags))
            {
                return tags.Values.FirstOrDefault();
            }
            return null;
        }
    }
}

public class NameIdPair
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;
}

public class BaseItemPerson
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Role")]
    public string? Role { get; set; }

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryImageTag")]
    public string? PrimaryImageTag { get; set; }
}

public class UserItemDataDto
{
    [JsonPropertyName("PlaybackPositionTicks")]
    public long PlaybackPositionTicks { get; set; }

    [JsonPropertyName("PlayCount")]
    public int PlayCount { get; set; }

    [JsonPropertyName("IsFavorite")]
    public bool IsFavorite { get; set; }

    [JsonPropertyName("Played")]
    public bool Played { get; set; }

    [JsonPropertyName("UnplayedItemCount")]
    public int? UnplayedItemCount { get; set; }

    [JsonPropertyName("PlayedPercentage")]
    public double? PlayedPercentage { get; set; }
}

public class MediaStream
{
    [JsonPropertyName("Codec")]
    public string Codec { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("Language")]
    public string? Language { get; set; }

    [JsonPropertyName("DisplayTitle")]
    public string? DisplayTitle { get; set; }

    [JsonPropertyName("Index")]
    public int Index { get; set; }

    [JsonPropertyName("BitRate")]
    public int? BitRate { get; set; }

    [JsonPropertyName("Width")]
    public int? Width { get; set; }

    [JsonPropertyName("Height")]
    public int? Height { get; set; }

    [JsonPropertyName("Channels")]
    public int? Channels { get; set; }

    [JsonPropertyName("SampleRate")]
    public int? SampleRate { get; set; }

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("IsForced")]
    public bool IsForced { get; set; }

    [JsonPropertyName("DeliveryUrl")]
    public string? DeliveryUrl { get; set; }

    [JsonPropertyName("SupportsTranscoding")]
    public bool SupportsTranscoding { get; set; }

    [JsonPropertyName("VideoRange")]
    public string? VideoRange { get; set; }

    [JsonPropertyName("VideoRangeType")]
    public string? VideoRangeType { get; set; }

    [JsonPropertyName("VideoDoViTitle")]
    public string? VideoDoViTitle { get; set; }

    [JsonPropertyName("HDRFormat")]
    public string? HDRFormat { get; set; }
}

public class MediaSourceInfo
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("Size")]
    public long? Size { get; set; }

    [JsonPropertyName("Container")]
    public string Container { get; set; } = string.Empty;

    [JsonPropertyName("SupportsTranscoding")]
    public bool SupportsTranscoding { get; set; }

    [JsonPropertyName("SupportsDirectStream")]
    public bool SupportsDirectStream { get; set; }

    [JsonPropertyName("SupportsDirectPlay")]
    public bool SupportsDirectPlay { get; set; }

    [JsonPropertyName("TranscodingUrl")]
    public string? TranscodingUrl { get; set; }

    [JsonPropertyName("DirectStreamUrl")]
    public string? DirectStreamUrl { get; set; }

    [JsonPropertyName("MediaStreams")]
    public List<MediaStream> MediaStreams { get; set; } = new();

    [JsonPropertyName("Bitrate")]
    public int? Bitrate { get; set; }

    [JsonPropertyName("MediaAttachments")]
    public List<MediaAttachment> MediaAttachments { get; set; } = new();
}

public class MediaAttachment
{
    [JsonPropertyName("Codec")]
    public string Codec { get; set; } = string.Empty;

    [JsonPropertyName("CodecTag")]
    public string? CodecTag { get; set; }

    [JsonPropertyName("Language")]
    public string? Language { get; set; }

    [JsonPropertyName("DeliveryUrl")]
    public string? DeliveryUrl { get; set; }
}

public class ItemsResult
{
    [JsonPropertyName("Items")]
    public List<BaseItemDto> Items { get; set; } = new();

    [JsonPropertyName("TotalRecordCount")]
    public int TotalRecordCount { get; set; }
}

public class ViewItem
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("CollectionType")]
    public string? CollectionType { get; set; }

    [JsonPropertyName("ImageTags")]
    public Dictionary<string, string> ImageTags { get; set; } = new();
}

public class PlaybackInfoResponse
{
    [JsonPropertyName("MediaSources")]
    public List<MediaSourceInfo> MediaSources { get; set; } = new();
}

public class TranscodingProfile
{
    [JsonPropertyName("Container")]
    public string Container { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("VideoCodec")]
    public string VideoCodec { get; set; } = string.Empty;

    [JsonPropertyName("AudioCodec")]
    public string AudioCodec { get; set; } = string.Empty;

    [JsonPropertyName("Context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("Protocol")]
    public string Protocol { get; set; } = string.Empty;
}

public class DeviceProfileBuilder
{
    public static string GetXboxProfileJson()
    {
        return """
        {
          "DeviceProfile": {
            "Name": "Jellyfin Xbox",
            "MaxStreamingBitrate": 120000000,
            "MaxStaticBitrate": 120000000,
            "MusicStreamingTranscodingBitrate": 192000,
            "DirectPlayProfiles": [
              {
                "Container": "mkv,mov,mp4,avi,ts,wmv,mpg,mpeg,webm",
                "Type": "Video",
                "VideoCodec": "av1,h264,hevc,vp9",
                "AudioCodec": "opus,aac,flac,wav,vorbis,wma,ac3,eac3,dts,mp2,mp3,truehd"
              },
              {
                "Container": "mp3,aac,flac,wav,wma,ogg,opus,aiff,m4a,webm",
                "Type": "Audio"
              }
            ],
            "TranscodingProfiles": [
              {
                "Container": "mp4",
                "Type": "Video",
                "VideoCodec": "av1,h264",
                "AudioCodec": "opus,aac",
                "Context": "Streaming",
                "Protocol": "hls"
              },
              {
                "Container": "ts",
                "Type": "Video",
                "VideoCodec": "h264",
                "AudioCodec": "opus,aac",
                "Context": "Streaming",
                "Protocol": "hls"
              }
            ],
            "SubtitleProfiles": [
              { "Format": "srt", "Method": "External" },
              { "Format": "ass", "Method": "External" },
              { "Format": "ssa", "Method": "External" },
              { "Format": "sub", "Method": "Embed" },
              { "Format": "subrip", "Method": "Embed" },
              { "Format": "vtt", "Method": "Embed" },
              { "Format": "pgs", "Method": "Embed" },
              { "Format": "pgssub", "Method": "Embed" }
            ]
          }
        }
        """;
    }
}
