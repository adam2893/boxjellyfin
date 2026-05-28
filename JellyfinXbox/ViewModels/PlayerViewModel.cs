using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.Media.Core;
using Windows.Media.Playback;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using System.Linq;

namespace JellyfinXbox.ViewModels;

public class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private readonly TrackSelectionViewModel _trackSelection;
    private readonly DispatcherQueue _dispatcher;

    private MediaPlayer? _player;
    private Timer? _reportTimer;
    private string? _currentItemId;
    private MediaSourceInfo? _currentMediaSource;

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
    public MediaPlayer? Player => _player;

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

        PlayCommand = new RelayCommand(Play);
        PauseCommand = new RelayCommand(Pause);
        TogglePlayPauseCommand = new RelayCommand(TogglePlayPause);
        SeekForwardCommand = new RelayCommand(SeekForward);
        SeekBackwardCommand = new RelayCommand(SeekBackward);
        SeekToCommand = new RelayCommand<TimeSpan>(SeekTo);
        StopCommand = new RelayCommand(Stop);
    }

    public async Task LoadAsync(string itemId)
    {
        var item = await _api.GetItemAsync(itemId);
        if (item == null) return;

        _currentItemId = itemId;
        Title = item.Name;

        if (item.Type == "Episode")
        {
            Subtitle = !string.IsNullOrEmpty(item.SeriesName)
                ? $"S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2} — {item.SeriesName}"
                : $"S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2}";
        }
        else if (item.ProductionYear.HasValue)
        {
            Subtitle = item.ProductionYear.Value.ToString();
        }

        var backdropId = item.BackdropImageTags.Count > 0 ? item.Id : item.ParentBackdropItemId;
        if (!string.IsNullOrEmpty(backdropId))
            BackdropUrl = $"{_api.ServerUrl}{_api.GetBackdropUrl(backdropId, maxWidth: 1920)}";

        var maxBitrate = _api.Settings.MaxStreamingBitrate;
        IsBuffering = true;
        var playbackInfo = await _api.GetPlaybackInfoAsync(itemId, maxBitrate);

        if (playbackInfo?.MediaSources == null || playbackInfo.MediaSources.Count == 0)
        {
            IsBuffering = false;
            return;
        }

        var source = SelectMediaSource(playbackInfo.MediaSources);
        _currentMediaSource = source;

        var mediaUrl = BuildMediaUrl(itemId, source);
        Debug.WriteLine($"[Player] Playing: {mediaUrl}");

        InitializePlayer();
        _player!.Source = MediaSource.CreateFromUri(new Uri(mediaUrl));

        _trackSelection.LoadTracks(source);
        _ = _api.ReportPlaybackStartAsync(itemId);

        _reportTimer = new Timer(async _ => await ReportProgress(), null, 5000, 10000);
    }

    private MediaSourceInfo SelectMediaSource(List<MediaSourceInfo> sources)
    {
        var directPlay = sources
            .Where(s => s.SupportsDirectPlay)
            .OrderByDescending(s =>
            {
                var video = s.MediaStreams.FirstOrDefault(ms => ms.Type == "Video");
                return video?.Codec switch
                {
                    "av1" => 3, "hevc" or "h265" => 2, "h264" or "avc" => 1, _ => 0
                };
            })
            .FirstOrDefault();

        if (directPlay != null)
        {
            IsDirectPlay = true;
            UpdateCodecInfo(directPlay);
            return directPlay;
        }

        var transcode = sources.OrderByDescending(s => s.SupportsTranscoding).FirstOrDefault() ?? sources.First();
        IsDirectPlay = false;
        UpdateCodecInfo(transcode);
        return transcode;
    }

    private void UpdateCodecInfo(MediaSourceInfo source)
    {
        var video = source.MediaStreams.FirstOrDefault(ms => ms.Type == "Video");
        var audio = source.MediaStreams.FirstOrDefault(ms => ms.Type == "Audio");

        if (video != null)
        {
            CurrentVideoCodec = video.Codec?.ToUpperInvariant() ?? "Unknown";
            if (video.Width.HasValue && video.Height.HasValue)
                CurrentResolution = $"{video.Width.Value}x{video.Height.Value}";
            var vr = video.VideoRangeType ?? video.VideoRange;
            IsHdr = !string.IsNullOrEmpty(vr) && vr.Contains("HDR", StringComparison.OrdinalIgnoreCase);
        }
        if (audio != null)
            CurrentAudioCodec = audio.Codec?.ToUpperInvariant() ?? "Unknown";
    }

    private string BuildMediaUrl(string itemId, MediaSourceInfo source)
    {
        var baseUrl = _api.ServerUrl;
        if (source.SupportsDirectPlay && !string.IsNullOrEmpty(source.DirectStreamUrl))
            return $"{baseUrl}{source.DirectStreamUrl}&ApiKey={_api.AccessToken}";
        if (source.SupportsDirectStream)
            return $"{baseUrl}/Videos/{itemId}/stream?static=true&MediaSourceId={source.Id}&ApiKey={_api.AccessToken}";
        if (!string.IsNullOrEmpty(source.TranscodingUrl))
            return $"{baseUrl}{source.TranscodingUrl}&ApiKey={_api.AccessToken}";
        return $"{baseUrl}/Videos/{itemId}/stream?static=true&MediaSourceId={source.Id}&ApiKey={_api.AccessToken}";
    }

    private void OnTracksChanged(MediaStream? audio, MediaStream? subtitle)
    {
        if (_player == null || _currentMediaSource == null || _currentItemId == null) return;
        var itemId = _currentItemId;
        var source = _currentMediaSource;
        var currentPosition = _player.PlaybackSession.Position;

        var newUrl = _api.BuildTranscodeUrlWithTracks(itemId, source,
            audio?.Index, subtitle?.Index);
        Debug.WriteLine($"[Player] Switching tracks");

        _player.Source = MediaSource.CreateFromUri(new Uri(newUrl));
        var seekTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        seekTimer.Tick += (s, e) =>
        {
            seekTimer.Stop();
            try { _player.PlaybackSession.Position = currentPosition; }
            catch { }
        };
        seekTimer.Start();
    }

    private void InitializePlayer()
    {
        if (_player != null)
        {
            _player.MediaOpened -= Player_MediaOpened;
            _player.PlaybackSession.PlaybackStateChanged -= Playback_PlaybackStateChanged;
            _player.PlaybackSession.PositionChanged -= Playback_PositionChanged;
            _player.MediaEnded -= Player_MediaEnded;
            _player.MediaFailed -= Player_MediaFailed;
            _player.Dispose();
        }

        _player = new MediaPlayer();
        _player.AutoPlay = true;
        _player.AudioCategory = MediaPlayerAudioCategory.Movie;
        _player.CommandManager.IsEnabled = false;

        _player.MediaOpened += Player_MediaOpened;
        _player.PlaybackSession.PlaybackStateChanged += Playback_PlaybackStateChanged;
        _player.PlaybackSession.PositionChanged += Playback_PositionChanged;
        _player.MediaEnded += Player_MediaEnded;
        _player.MediaFailed += Player_MediaFailed;
    }

    private void Player_MediaOpened(MediaPlayer sender, object args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Duration = sender.PlaybackSession.NaturalDuration;
            DurationDisplay = FormatTime(Duration);
            HasVideo = sender.PlaybackSession.NaturalVideoHeight > 0;
            IsBuffering = false;
        });
    }

    private void Playback_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            IsPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
            IsBuffering = sender.PlaybackState == MediaPlaybackState.Buffering;
        });
    }

    private void Playback_PositionChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Position = sender.Position;
            PositionDisplay = FormatTime(Position);
            BufferingProgress = sender.BufferingProgress * 100;
        });
    }

    private async void Player_MediaEnded(MediaPlayer sender, object args)
    {
        Debug.WriteLine("[Player] Media ended.");
        IsPlaying = false;
        if (_currentItemId != null)
        {
            await _api.ReportPlaybackStoppedAsync(_currentItemId, Duration.Ticks);
            await _api.MarkPlayedAsync(_currentItemId);
        }
        _reportTimer?.Dispose();
    }

    private async void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Debug.WriteLine($"[Player] Failed: {args.Error}, Extended: {args.ExtendedErrorCode}");
        IsBuffering = false;
        IsPlaying = false;
        if (_currentItemId != null)
            await _api.ReportPlaybackStoppedAsync(_currentItemId, Position.Ticks);
    }

    private async Task ReportProgress()
    {
        if (_currentItemId != null && IsPlaying)
            await _api.ReportPlaybackProgressAsync(_currentItemId, Position.Ticks, !IsPlaying);
    }

    private void Play() => _player?.Play();
    private void Pause() => _player?.Pause();

    private void TogglePlayPause()
    {
        if (_player == null) return;
        if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            _player.Pause();
        else
            _player.Play();
    }

    private void SeekForward() { if (_player != null) _player.PlaybackSession.Position += TimeSpan.FromSeconds(10); }
    private void SeekBackward() { if (_player != null) _player.PlaybackSession.Position -= TimeSpan.FromSeconds(10); }

    private void SeekTo(TimeSpan position)
    {
        if (_player != null)
            _player.PlaybackSession.Position = position;
    }

    private void Stop()
    {
        if (_currentItemId != null)
            _ = _api.ReportPlaybackStoppedAsync(_currentItemId, Position.Ticks);
        _player?.Pause();
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
        _player?.Dispose();
    }
}
