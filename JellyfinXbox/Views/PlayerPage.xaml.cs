using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using System.Linq;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class PlayerPage : Page
{
    public PlayerViewModel ViewModel { get; }
    private DispatcherTimer? _hideTimer;
    private DispatcherTimer? _positionTimer;
    private string? _pendingItemId;

    public PlayerPage(PlayerViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _hideTimer.Tick += (s, e) =>
        {
            TransportOverlay.Opacity = 0;
            _hideTimer.Stop();
        };

        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _positionTimer.Tick += (s, e) =>
        {
            if (ViewModel.Duration.TotalSeconds < 0.5) return;
            var realPos = MediaElement.Position.TotalSeconds;
            var sliderDelta = Math.Abs(SeekSlider.Value - realPos);

            // Only update slider if we're not being dragged (delta < 1s = timer update, > 1s = user dragging)
            if (sliderDelta < 1.5)
            {
                SeekSlider.Maximum = ViewModel.Duration.TotalSeconds;
                SeekSlider.Value = realPos;
            }
            ViewModel.Position = MediaElement.Position;
            ViewModel.PositionDisplay = FormatTime(MediaElement.Position);
        };

        Loaded += OnLoaded;
    }

    public void Initialize(string itemId)
    {
        _pendingItemId = itemId;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_pendingItemId == null) return;

        App.Log($"[Player] OnLoaded — itemId={_pendingItemId}");
        try
        {
            // Hook MediaElement events
            MediaElement.MediaOpened += Media_MediaOpened;
            MediaElement.MediaFailed += Media_MediaFailed;
            MediaElement.MediaEnded += Media_MediaEnded;
            MediaElement.CurrentStateChanged += Media_CurrentStateChanged;
            App.Log("[Player] MediaElement events hooked");

            // Hook track changes — switch URI when audio/subtitle tracks change
            ViewModel.TrackUriChanged += (newUri) =>
            {
                var pos = MediaElement.Position;
                App.Log($"[Player] Track switch: {newUri}");
                MediaElement.Source = newUri;
                // Restore position after brief delay
                var restoreTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                restoreTimer.Tick += (s2, e2) =>
                {
                    restoreTimer.Stop();
                    try { MediaElement.Position = pos; } catch { }
                };
                restoreTimer.Start();
            };

            // Seek bar: seek only on thumb release, not during drag
            SeekSlider.ManipulationCompleted += (s, args) => DoSeek();
            SeekSlider.PointerCaptureLost += (s, args) => DoSeek();

            // Prepare URL and metadata
            var url = await ViewModel.PrepareUrlAsync(_pendingItemId);
            if (url == null)
            {
                App.LogWarn("[Player] PrepareUrlAsync returned null");
                return;
            }

            App.Log($"[Player] Setting Source: {url}");
            MediaElement.Source = url;
            ViewModel.IsBuffering = true;
            App.Log("[Player] Source set, waiting for MediaOpened...");
        }
        catch (Exception ex)
        {
            App.LogWarn($"[Player] EX: {ex.GetType().Name}: {ex.Message}");
            App.LogWarn($"[Player] Stack: {ex.StackTrace}");
        }
    }

    // ═════ MediaElement Events ═════

    private void Media_MediaOpened(object sender, RoutedEventArgs e)
    {
        App.Log($"[Player] >>> MediaOpened: {MediaElement.NaturalVideoWidth}x{MediaElement.NaturalVideoHeight} <<<");
        // Duration is already set from API (streaming sources don't report it via MediaElement)
        ViewModel.HasVideo = MediaElement.NaturalVideoHeight > 0;
        ViewModel.IsBuffering = false;
        _isSeeking = false;
    }

    private void Media_CurrentStateChanged(object sender, RoutedEventArgs e)
    {
        App.Log($"[Player] >>> State: {MediaElement.CurrentState} <<<");
        ViewModel.IsPlaying = MediaElement.CurrentState == MediaElementState.Playing;
        ViewModel.IsBuffering = MediaElement.CurrentState == MediaElementState.Buffering;
        PlayPauseIcon.Glyph = ViewModel.IsPlaying ? "\uE769" : "\uE768";

        if (MediaElement.CurrentState == MediaElementState.Playing)
            _positionTimer?.Start();
        else
            _positionTimer?.Stop();
    }

    private void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        App.LogWarn($"[Player] >>> MediaFailed: {e.ErrorMessage} <<<");
        ViewModel.IsBuffering = false;
        ViewModel.IsPlaying = false;
    }

    private void Media_MediaEnded(object sender, RoutedEventArgs e)
    {
        App.Log("[Player] >>> MediaEnded <<<");
        ViewModel.IsPlaying = false;
    }

    private DateTime _lastSeek;
    private bool _isSeeking;

    private async void DoSeek()
    {
        // Debounce: both ManipulationCompleted and PointerCaptureLost can fire
        if ((DateTime.UtcNow - _lastSeek).TotalMilliseconds < 300) return;
        if (_isSeeking) return;
        _lastSeek = DateTime.UtcNow;

        var newPos = TimeSpan.FromSeconds(SeekSlider.Value);
        var wasPlaying = MediaElement.CurrentState == MediaElementState.Playing;
        App.Log($"[Player] Seek to {newPos} (wasPlaying={wasPlaying})");

        // MediaElement.Position setter is unreliable for HTTP streaming on UWP.
        // Instead, restart the stream with StartTimeTicks in the URL.
        var seekUrl = ViewModel.BuildSeekUrl(newPos);
        if (seekUrl == null) return;

        try
        {
            _isSeeking = true;
            _positionTimer?.Stop();
            ViewModel.IsBuffering = true;
            MediaElement.Stop();
            MediaElement.Source = seekUrl;
            ViewModel.Position = newPos;
            ViewModel.PositionDisplay = FormatTime(newPos);
            if (wasPlaying)
                MediaElement.Play();
            App.Log($"[Player] Seek restart: Source set, wasPlaying={wasPlaying}");
        }
        catch (Exception ex)
        {
            App.LogWarn($"[Player] DoSeek EX: {ex.Message}");
        }
    }

    // ═════ Transport Controls ═════

    private void ShowTransport()
    {
        TransportOverlay.Opacity = 1;
        _hideTimer?.Start();
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        App.Log($"[Player] PlayPause: current={MediaElement.CurrentState}");
        if (MediaElement.CurrentState == MediaElementState.Playing)
        {
            MediaElement.Pause();
        }
        else if (MediaElement.CurrentState == MediaElementState.Paused)
        {
            App.Log("[Player] Calling Play()...");
            MediaElement.Play();
            App.Log($"[Player] After Play(): {MediaElement.CurrentState}");
        }
        else if (MediaElement.CurrentState == MediaElementState.Stopped)
        {
            App.Log("[Player] Calling Play() from Stopped...");
            MediaElement.Play();
            App.Log($"[Player] After Play(): {MediaElement.CurrentState}");
        }
        ShowTransport();
    }

    private void SeekForward_Click(object sender, RoutedEventArgs e)
    {
        MediaElement.Position += TimeSpan.FromSeconds(10);
        ShowTransport();
    }
    private void SeekBack_Click(object sender, RoutedEventArgs e)
    {
        MediaElement.Position -= TimeSpan.FromSeconds(10);
        ShowTransport();
    }
    private void Rewind_Click(object sender, RoutedEventArgs e)
    {
        MediaElement.Position -= TimeSpan.FromSeconds(20);
        ShowTransport();
    }
    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        MediaElement.Position += TimeSpan.FromSeconds(20);
        ShowTransport();
    }
    private void Back_Click(object sender, RoutedEventArgs e) => ViewModel.StopCommand.Execute(null);

    // ═════ Track Selection ═════

    private void ToggleTrackPanel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TrackSelection.ToggleVisibilityCommand.Execute(null);
    }

    private void CloseTrackPanel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TrackSelection.HideCommand.Execute(null);
    }

    private void AudioListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is MediaStream track)
        {
            ViewModel.TrackSelection.SelectedAudioTrack = track;
            ShowTransport();
        }
    }

    private void SubtitleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is MediaStream track)
        {
            ViewModel.TrackSelection.SelectedSubtitleTrack = track;
            ShowTransport();
        }
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e) => ShowTransport();

    private static string FormatTime(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}
