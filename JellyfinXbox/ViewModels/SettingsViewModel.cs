using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using System.Linq;
using JellyfinXbox.Views;

namespace JellyfinXbox.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private readonly UserSettings _settings;

    private string _serverUrl = "";
    private string _serverName = "";
    private string _serverVersion = "";
    private string _userName = "";
    private string _clientVersion = "1.0.6.35";
    private string _logPath = "";
    private int _selectedBitrateIndex;
    private int _selectedSubtitleModeIndex;
    private int _subtitleFontSize;
    private bool _autoPlayNextEpisode;
    private bool _preferForeignSubtitles;

    public string ServerUrl { get => _serverUrl; set => SetProperty(ref _serverUrl, value); }
    public string ServerName { get => _serverName; set => SetProperty(ref _serverName, value); }
    public string ServerVersion { get => _serverVersion; set => SetProperty(ref _serverVersion, value); }
    public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
    public string ClientVersion => _clientVersion;
    public string LogPath { get => _logPath; set => SetProperty(ref _logPath, value); }

    public int SelectedBitrateIndex
    {
        get => _selectedBitrateIndex;
        set { if (SetProperty(ref _selectedBitrateIndex, value)) OnBitrateChanged(value); }
    }

    public int SelectedSubtitleModeIndex
    {
        get => _selectedSubtitleModeIndex;
        set { if (SetProperty(ref _selectedSubtitleModeIndex, value)) OnSubtitleModeChanged(value); }
    }

    public int SubtitleFontSize
    {
        get => _subtitleFontSize;
        set { if (SetProperty(ref _subtitleFontSize, value)) ApplySettings(); }
    }

    public bool AutoPlayNextEpisode
    {
        get => _autoPlayNextEpisode;
        set { if (SetProperty(ref _autoPlayNextEpisode, value)) ApplySettings(); }
    }

    public bool PreferForeignSubtitles
    {
        get => _preferForeignSubtitles;
        set { if (SetProperty(ref _preferForeignSubtitles, value)) ApplySettings(); }
    }

    public ObservableCollection<string> BitrateLabels { get; } = new();
    public ObservableCollection<string> SubtitleModes { get; } = new();

    private readonly long[] _bitrateValues;
    private readonly string[] _subtitleModeValues = { "Default", "On", "Off", "OnlyForced" };

    public ICommand GoBackCommand { get; }
    public ICommand LogoutCommand { get; }

    public SettingsViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        _settings = api.Settings;

        GoBackCommand = new RelayCommand(GoBack);
        LogoutCommand = new RelayCommand(Logout);

        foreach (var (label, _) in UserSettings.BitratePresets)
            BitrateLabels.Add(label);
        _bitrateValues = UserSettings.BitratePresets.Select(p => p.Value).ToArray();

        foreach (var mode in _subtitleModeValues)
        {
            SubtitleModes.Add(mode switch
            {
                "Default" => "Default",
                "On" => "Always On",
                "Off" => "Off",
                "OnlyForced" => "Forced Only",
                _ => mode
            });
        }

        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        var s = _api.Settings;

        SelectedBitrateIndex = Array.FindIndex(_bitrateValues, v => v == s.MaxStreamingBitrate);
        if (SelectedBitrateIndex < 0) SelectedBitrateIndex = 0;

        SelectedSubtitleModeIndex = Array.IndexOf(_subtitleModeValues, s.SubtitleMode);
        if (SelectedSubtitleModeIndex < 0) SelectedSubtitleModeIndex = 0;

        SubtitleFontSize = s.SubtitleFontSize;
        AutoPlayNextEpisode = s.AutoPlayNextEpisode;
        PreferForeignSubtitles = s.PreferForeignSubtitles;

        ServerUrl = _api.ServerUrl ?? "";
        ServerName = _api.ServerInfo?.ServerName ?? "";
        ServerVersion = _api.ServerInfo?.Version ?? "";
        UserName = _api.CurrentUser?.Name ?? "";
        LogPath = App.LogPath ?? "(unavailable)";
    }

    private void OnBitrateChanged(int value)
    {
        if (value >= 0 && value < _bitrateValues.Length)
        {
            _settings.MaxStreamingBitrate = _bitrateValues[value];
            ApplySettings();
        }
    }

    private void OnSubtitleModeChanged(int value)
    {
        if (value >= 0 && value < _subtitleModeValues.Length)
        {
            _settings.SubtitleMode = _subtitleModeValues[value];
            ApplySettings();
        }
    }

    private void ApplySettings()
    {
        _api.UpdateSettings(_settings);
    }

    private void GoBack() => _nav.GoBack();

    private void Logout()
    {
        _api.Logout();
        _nav.NavigateTo(typeof(LoginPage));
    }
}
