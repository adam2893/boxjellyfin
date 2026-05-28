using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using System.Linq;

namespace JellyfinXbox.ViewModels;

public class TrackSelectionViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private MediaSourceInfo? _currentSource;

    private bool _isVisible;
    private MediaStream? _selectedAudioTrack;
    private MediaStream? _selectedSubtitleTrack;
    private bool _subtitlesEnabled = true;

    public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

    public MediaStream? SelectedAudioTrack
    {
        get => _selectedAudioTrack;
        set { if (SetProperty(ref _selectedAudioTrack, value)) RaiseTracksChanged(); }
    }

    public MediaStream? SelectedSubtitleTrack
    {
        get => _selectedSubtitleTrack;
        set
        {
            if (SetProperty(ref _selectedSubtitleTrack, value))
            {
                SubtitlesEnabled = value != null && value.Index != -1;
                RaiseTracksChanged();
            }
        }
    }

    public bool SubtitlesEnabled { get => _subtitlesEnabled; set => SetProperty(ref _subtitlesEnabled, value); }

    public ObservableCollection<MediaStream> AudioTracks { get; } = new();
    public ObservableCollection<MediaStream> SubtitleTracks { get; } = new();

    public bool HasAudioTracks => AudioTracks.Count > 1;
    public bool HasSubtitleTracks => SubtitleTracks.Count > 0;

    public event Action<MediaStream?, MediaStream?>? OnTracksChanged;

    public ICommand ToggleSubtitlesCommand { get; }
    public ICommand ShowCommand { get; }
    public ICommand HideCommand { get; }
    public ICommand ToggleVisibilityCommand { get; }

    public TrackSelectionViewModel(JellyfinApiClient api)
    {
        _api = api;
        ToggleSubtitlesCommand = new RelayCommand(ToggleSubtitles);
        ShowCommand = new RelayCommand(Show);
        HideCommand = new RelayCommand(Hide);
        ToggleVisibilityCommand = new RelayCommand(ToggleVisibility);
    }

    public void LoadTracks(MediaSourceInfo source)
    {
        _currentSource = source;

        AudioTracks.Clear();
        SubtitleTracks.Clear();

        var audioStreams = JellyfinApiClient.GetAudioStreams(source);
        foreach (var a in audioStreams) AudioTracks.Add(a);

        var subStreams = JellyfinApiClient.GetSubtitleStreams(source);
        SubtitleTracks.Add(new MediaStream
        {
            Index = -1,
            Type = "Subtitle",
            DisplayTitle = "Off",
            Language = "off",
            Codec = "off",
            IsDefault = false
        });
        foreach (var s in subStreams) SubtitleTracks.Add(s);

        SelectedAudioTrack = AudioTracks.FirstOrDefault(a => a.IsDefault) ?? AudioTracks.FirstOrDefault();
        var defaultSub = SubtitleTracks.FirstOrDefault(s => s.Index != -1 && s.IsDefault);
        SelectedSubtitleTrack = defaultSub ?? SubtitleTracks.FirstOrDefault();
        SubtitlesEnabled = defaultSub != null && SelectedSubtitleTrack?.Index != -1;
    }

    private void RaiseTracksChanged()
    {
        var sub = SubtitlesEnabled ? SelectedSubtitleTrack : SubtitleTracks.FirstOrDefault(s => s.Index == -1);
        OnTracksChanged?.Invoke(SelectedAudioTrack, sub);
    }

    private void ToggleSubtitles()
    {
        SubtitlesEnabled = !SubtitlesEnabled;
        if (SubtitlesEnabled)
            SelectedSubtitleTrack = SubtitleTracks.FirstOrDefault(s => s.Index != -1) ?? SelectedSubtitleTrack;
        else
            SelectedSubtitleTrack = SubtitleTracks.FirstOrDefault(s => s.Index == -1);
    }

    private void Show() => IsVisible = true;
    private void Hide() => IsVisible = false;
    private void ToggleVisibility() => IsVisible = !IsVisible;
}
