using System;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.Media.Playback;
using System.Linq;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;
using System.Threading.Tasks;

namespace JellyfinXbox.Views;

public sealed partial class PlayerPage : Page
{
    public PlayerViewModel ViewModel { get; }
    private DispatcherTimer? _hideTimer;

    public PlayerPage(PlayerViewModel viewModel) { ViewModel = viewModel; InitializeComponent();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _hideTimer.Tick += (s, e) =>
        {
            TransportOverlay.Opacity = 0;
            _hideTimer.Stop();
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string itemId)
        {
            var player = ViewModel.Player;
            if (player != null)
                MediaPlayerElement.SetMediaPlayer(player);

            _ = LoadSafeAsync(itemId);
        }
    }

    private async Task LoadSafeAsync(string id)
    {
        try { await ViewModel.LoadAsync(id); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Player load failed: {ex.Message}"); }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Dispose();
    }

    private void ShowTransport()
    {
        TransportOverlay.Opacity = 1;
        _hideTimer?.Start();
    }

    // â•â•â• Transport Controls â•â•â•

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

    // â•â•â• Track Selection â•â•â•

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

    // Show transport on any interaction
    protected override void OnPointerMoved(PointerRoutedEventArgs e) => ShowTransport();
}
