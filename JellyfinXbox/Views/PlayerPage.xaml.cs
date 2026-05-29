using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.Media.Playback;
using System.Linq;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class PlayerPage : Page
{
    public PlayerViewModel ViewModel { get; }
    private DispatcherTimer? _hideTimer;
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

        Loaded += OnLoaded;
    }

    public void Initialize(string itemId)
    {
        _pendingItemId = itemId;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_pendingItemId != null)
        {
            var player = ViewModel.Player;
            if (player != null)
                MediaPlayerElement.SetMediaPlayer(player);

            try { await ViewModel.LoadAsync(_pendingItemId); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Player load failed: {ex.Message}"); }
        }
    }

    private void ShowTransport()
    {
        TransportOverlay.Opacity = 1;
        _hideTimer?.Start();
    }

    // ═════ Transport Controls ═════

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TogglePlayPauseCommand.Execute(null);
        PlayPauseIcon.Glyph = ViewModel.IsPlaying ? "\uE769" : "\uE768";
        ShowTransport();
    }

    private void SeekForward_Click(object sender, RoutedEventArgs e) { ViewModel.SeekForwardCommand.Execute(null); ShowTransport(); }
    private void SeekBack_Click(object sender, RoutedEventArgs e) { ViewModel.SeekBackwardCommand.Execute(null); ShowTransport(); }
    private void Rewind_Click(object sender, RoutedEventArgs e) { ViewModel.SeekBackwardCommand.Execute(null); ViewModel.SeekBackwardCommand.Execute(null); ShowTransport(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { ViewModel.SeekForwardCommand.Execute(null); ViewModel.SeekForwardCommand.Execute(null); ShowTransport(); }
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
}
