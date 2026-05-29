using System;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;
using DispatcherQueue = Windows.System.DispatcherQueue;

namespace JellyfinXbox.ViewModels;

/// <summary>
/// Step 1: Server connection. Enter URL, verify connectivity, proceed to login.
/// Split from the monolithic LoginPage for Xbox gamepad accessibility.
/// </summary>
public class ConnectViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;

    private string _serverUrl = "";
    private bool _isLoading;
    private string? _errorMessage;
    private string? _serverName;
    private string? _serverVersion;
    private bool _isConnected;
    private readonly DispatcherQueue _dispatcher;

    public string ServerUrl { get => _serverUrl; set => SetProperty(ref _serverUrl, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public string? ServerName { get => _serverName; set => SetProperty(ref _serverName, value); }
    public string? ServerVersion { get => _serverVersion; set => SetProperty(ref _serverVersion, value); }
    public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }

    public ICommand ConnectCommand { get; }
    public ICommand ContinueCommand { get; }
    public ICommand QuickConnectCommand { get; }

    public ConnectViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        _dispatcher = Windows.System.DispatcherQueue.GetForCurrentThread();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        ContinueCommand = new RelayCommand(ContinueToLogin);
        QuickConnectCommand = new RelayCommand(OpenQuickConnect);
    }

    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(_serverUrl))
        {
            _dispatcher.TryEnqueue(() => ErrorMessage = "Enter a server URL.");
            return;
        }

        var url = _serverUrl.Trim();
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        _dispatcher.TryEnqueue(() =>
        {
            ServerUrl = url;
            IsLoading = true;
            ErrorMessage = null;
            IsConnected = false;
            ServerName = null;
            ServerVersion = null;
        });

        _api.SetServerUrl(url);

        var (info, error) = await _api.GetServerInfoAsync();

        _dispatcher.TryEnqueue(() =>
        {
            IsLoading = false;

            if (info == null)
            {
                ErrorMessage = error ?? "Cannot connect to server. Check the URL.";
                return;
            }

            ServerName = info.ServerName;
            ServerVersion = info.Version;
            IsConnected = true;
            ErrorMessage = null;
        });
    }

    private void ContinueToLogin()
    {
        if (!IsConnected) return;
        _nav.NavigateTo(typeof(LoginPage));
    }

    private void OpenQuickConnect()
    {
        if (!IsConnected) return;
        _nav.NavigateTo(typeof(QuickConnectPage));
    }
}
