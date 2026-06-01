using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;

namespace JellyfinXbox.ViewModels;

public class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private readonly TrackSelectionViewModel _trackSelection;
    private readonly DispatcherQueue _dispatcher;

    private Timer? _reportTimer;
    private string? _currentItemId;
    private MediaSourceInfo? _currentMediaSource;
    private Uri? _currentUri;
    private int _currentAudioIndex = -1;
    private int _currentSubIndex = -1;

    // Fired when tracks change — PlayerPage should set MediaElement.Source to new URI
    public event Action<Uri>? TrackUriChanged;

    private bool _isPlaying;
    private bool _isBuffering;
    private bool _hasVideo;
    private string _title = "";
    private string _subtitle = "";
    private TimeSpan _position;
    private TimeSpan _duration;
    private string _positionDisplay = "0:00";
    private string _durationDisplay = "0:00";
    private string _backdropUrl = "";
    private double _bufferingProgress;
    private string _currentVideoCodec = "";
    private string _currentAudioCodec = "";
    private string _currentResolution = "";
    private bool _isHdr;
    private bool _isDirectPlay;

    public bool IsPlaying { get => _isPlaying; set => SetProperty(ref _isPlaying, value); }
    public bool IsBuffering { get => _isBuffering; set => SetProperty(ref _isBuffering, value); }
    public bool HasVideo { get => _hasVideo; set => SetProperty(ref _hasVideo, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Subtitle { get => _subtitle; set => SetProperty(ref _subtitle, value); }
    public TimeSpan Position { get => _position; set => SetProperty(ref _position, value); }
    public TimeSpan Duration { get => _duration; set => SetProperty(ref _duration, value); }
    public string PositionDisplay { get => _positionDisplay; set => SetProperty(ref _positionDisplay, value); }
    public string DurationDisplay { get => _durationDisplay; set => SetProperty(ref _durationDisplay, value); }
    public string BackdropUrl { get => _backdropUrl; set => SetProperty(ref _backdropUrl, value); }
    public double BufferingProgress { get => _bufferingProgress; set => SetProperty(ref _bufferingProgress, value); }
    public string CurrentVideoCodec { get => _currentVideoCodec; set => SetProperty(ref _currentVideoCodec, value); }
    public string CurrentAudioCodec { get => _currentAudioCodec; set => SetProperty(ref _currentAudioCodec, value); }
    public string CurrentResolution { get => _currentResolution; set => SetProperty(ref _currentResolution, value); }
    public bool IsHdr { get => _isHdr; set => SetProperty(ref _isHdr, value); }
    public bool IsDirectPlay { get => _isDirectPlay; set => SetProperty(ref _isDirectPlay, value); }

    public TrackSelectionViewModel TrackSelection => _trackSelection;

    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand TogglePlayPauseCommand { get; }
    public ICommand SeekForwardCommand { get; }
    public ICommand SeekBackwardCommand { get; }
    public ICommand SeekToCommand { get; }
    public ICommand StopCommand { get; }

    public PlayerViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _trackSelection = new TrackSelectionViewModel(api);
        _trackSelection.OnTracksChanged += OnTracksChanged;

        PlayCommand = new RelayCommand(() => { });
        PauseCommand = new RelayCommand(() => { });
        TogglePlayPauseCommand = new RelayCommand(() => { });
        SeekForwardCommand = new RelayCommand(() => { });
        SeekBackwardCommand = new RelayCommand(() => { });
        SeekToCommand = new RelayCommand<TimeSpan>(_ => { });
        StopCommand = new RelayCommand(Stop);
    }

    public async Task<Uri?> PrepareUrlAsync(string itemId)
    {
        App.Log($"[Player] ===== PrepareUrl: itemId={itemId} =====");
        try
        {
            var item = await _api.GetItemAsync(itemId);
            if (item == null) { App.LogWarn("[Player] Item null"); return null; }
            App.Log($"[Player] Item: {item.Name}, Type={item.Type}, RunTimeTicks={item.RunTimeTicks}");

            _currentItemId = itemId;
            Title = item.Name;
            Subtitle = item.Type == "Episode"
                ? (!string.IsNullOrEmpty(item.SeriesName)
                    ? $"S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2} — {item.SeriesName}"
                    : $"S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2}")
                : item.ProductionYear?.ToString() ?? "";

            // Set duration from API (not from MediaElement — streaming sources don't report it)
            if (item.RunTimeTicks.HasValue && item.RunTimeTicks.Value > 0)
            {
                Duration = TimeSpan.FromTicks(item.RunTimeTicks.Value);
                DurationDisplay = FormatTime(Duration);
                App.Log($"[Player] Duration from API: {DurationDisplay}");
            }

            var backdropId = item.BackdropImageTags.Count > 0 ? item.Id : item.ParentBackdropItemId;
            if (!string.IsNullOrEmpty(backdropId))
                BackdropUrl = $"{_api.ServerUrl}{_api.GetBackdropUrl(backdropId, maxWidth: 1920, tag: item.BackdropImageTags.FirstOrDefault())}";

            var playbackInfo = await _api.GetPlaybackInfoAsync(itemId, _api.Settings.MaxStreamingBitrate);
            if (playbackInfo?.MediaSources == null || playbackInfo.MediaSources.Count == 0)
            {
                App.LogWarn("[Player] No sources");
                return null;
            }

            for (int i = 0; i < playbackInfo.MediaSources.Count; i++)
            {
                var ms = playbackInfo.MediaSources[i];
                var v = ms.MediaStreams.FirstOrDefault(s => s.Type == "Video");
                var a = ms.MediaStreams.FirstOrDefault(s => s.Type == "Audio");
                var subs = ms.MediaStreams.Count(s => s.Type == "Subtitle");
                App.Log($"[Player] Src[{i}]: {ms.Container}, dp={ms.SupportsDirectPlay}, vc={v?.Codec}, ac={a?.Codec}, {v?.Width}x{v?.Height}, subs={subs}");
            }

            var source = SelectMediaSource(playbackInfo.MediaSources);
            _currentMediaSource = source;

            // Load audio/subtitle tracks
            _trackSelection.LoadTracks(source);
            App.Log($"[Player] Tracks: audio={_trackSelection.AudioTracks.Count}, subtitle={_trackSelection.SubtitleTracks.Count}");

            var url = BuildMediaUrl(itemId, source, item.UserData?.PlaybackPositionTicks);
            App.Log($"[Player] URL: {url}");
            _currentUri = new Uri(url);

            _ = _api.ReportPlaybackStartAsync(itemId);
            _reportTimer = new Timer(async _ => await ReportProgress(), null, 5000, 10000);
            App.Log("[Player] ===== PrepareUrl COMPLETE =====");
            return _currentUri;
        }
        catch (Exception ex)
        {
            App.LogWarn($"[Player] PrepareUrl EX: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private void OnTracksChanged(MediaStream? audio, MediaStream? subtitle)
    {
        if (_currentMediaSource == null || _currentItemId == null) return;

        var newUrl = _api.BuildTranscodeUrlWithTracks(_currentItemId, _currentMediaSource,
            audio?.Index, subtitle?.Index);
        App.Log($"[Player] Track switch URL: {newUrl}");
        _currentUri = new Uri(newUrl);
        TrackUriChanged?.Invoke(_currentUri);
    }

    private MediaSourceInfo SelectMediaSource(List<MediaSourceInfo> sources)
    {
        var dp = sources.Where(s => s.SupportsDirectPlay).FirstOrDefault();
        if (dp != null)
        {
            IsDirectPlay = true;
            UpdateCodecInfo(dp);
            return dp;
        }
        var fb = sources[0];
        IsDirectPlay = false;
        UpdateCodecInfo(fb);
        return fb;
    }

    private void UpdateCodecInfo(MediaSourceInfo source)
    {
        var v = source.MediaStreams.FirstOrDefault(ms => ms.Type == "Video");
        var a = source.MediaStreams.FirstOrDefault(ms => ms.Type == "Audio");
        if (v != null)
        {
            CurrentVideoCodec = v.Codec?.ToUpperInvariant() ?? "?";
            if (v.Width.HasValue && v.Height.HasValue)
                CurrentResolution = $"{v.Width.Value}x{v.Height.Value}";
            IsHdr = (v.VideoRangeType ?? v.VideoRange ?? "").Contains("HDR", StringComparison.OrdinalIgnoreCase);
        }
        if (a != null) CurrentAudioCodec = a.Codec?.ToUpperInvariant() ?? "?";
    }

    private string BuildMediaUrl(string itemId, MediaSourceInfo source, long? resumeTicks = null)
    {
        var baseUrl = _api.ServerUrl;
        string url;
        if (source.SupportsDirectPlay && !string.IsNullOrEmpty(source.DirectStreamUrl))
            url = $"{baseUrl}{source.DirectStreamUrl}&ApiKey={_api.AccessToken}";
        else
            url = $"{baseUrl}/Videos/{itemId}/stream?MediaSourceId={source.Id}&ApiKey={_api.AccessToken}";

        // Resume from saved position (Continue Watching)
        if (resumeTicks.HasValue && resumeTicks.Value > 0)
        {
            url += $"&startTimeTicks={resumeTicks.Value}";
            App.Log($"[Player] Resume position: {TimeSpan.FromTicks(resumeTicks.Value)}");
        }

        return url;
    }

    /// <summary>
    /// Builds a seek URL using Jellyfin's startTimeTicks (camelCase for ASP.NET Core model binding).
    /// </summary>
    public Uri? BuildSeekUrl(TimeSpan position)
    {
        if (_currentItemId == null || _currentMediaSource == null) return null;
        try
        {
            var baseUrl = BuildMediaUrl(_currentItemId, _currentMediaSource);
            var ticks = position.Ticks;
            // startTimeTicks must be camelCase — ASP.NET Core model binding ignores PascalCase
            var url = $"{baseUrl}&startTimeTicks={ticks}";
            App.Log($"[Player] BuildSeekUrl: {url}");
            return new Uri(url);
        }
        catch (Exception ex)
        {
            App.LogWarn($"[Player] BuildSeekUrl EX: {ex.Message}");
            return null;
        }
    }

    private async Task ReportProgress()
    {
        if (_currentItemId != null && IsPlaying)
            await _api.ReportPlaybackProgressAsync(_currentItemId, Position.Ticks, !IsPlaying);
    }

    private void Stop()
    {
        if (_currentItemId != null)
            _ = _api.ReportPlaybackStoppedAsync(_currentItemId, Position.Ticks);
        _reportTimer?.Dispose();
        _nav.GoBack();
    }

    private static string FormatTime(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    public void Dispose()
    {
        _reportTimer?.Dispose();
        if (_currentItemId != null && IsPlaying)
            _ = _api.ReportPlaybackStoppedAsync(_currentItemId, Position.Ticks);
    }
}
