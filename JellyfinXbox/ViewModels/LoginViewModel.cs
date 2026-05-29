using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;
using DispatcherQueue = Windows.System.DispatcherQueue;
using User = JellyfinClient.Models.User;

namespace JellyfinXbox.ViewModels;

/// <summary>
/// Step 2: User authentication. Shows public users, username/password, Quick Connect option.
/// Assumes server is already connected (ConnectViewModel handles that).
/// </summary>
public class LoginViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private readonly DispatcherQueue _dispatcher;

    private string _username = "";
    private string _password = "";
    private bool _isLoading;
    private string? _errorMessage;
    private string? _serverName;
    private bool _isQuickConnectEnabled;

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public string? ServerName { get => _serverName; set => SetProperty(ref _serverName, value); }
    public bool IsQuickConnectEnabled { get => _isQuickConnectEnabled; set => SetProperty(ref _isQuickConnectEnabled, value); }

    public ObservableCollection<User> PublicUsers { get; } = new();

    public ICommand LoginCommand { get; }
    public ICommand SelectUserCommand { get; }
    public ICommand QuickConnectCommand { get; }

    public LoginViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        _dispatcher = Windows.System.DispatcherQueue.GetForCurrentThread();
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        SelectUserCommand = new RelayCommand<User>(SelectUser);
        QuickConnectCommand = new RelayCommand(OpenQuickConnect);
    }

    /// <summary>
    /// Called when the page loads. Fetches public users and Quick Connect status.
    /// </summary>
    public async Task InitializeAsync()
    {
        _dispatcher.TryEnqueue(() =>
        {
            IsLoading = true;
            ErrorMessage = null;
            ServerName = _api.ServerInfo?.ServerName ?? _api.ServerUrl;
        });

        try
        {
            var users = await _api.GetPublicUsersAsync();
            _dispatcher.TryEnqueue(() =>
            {
                PublicUsers.Clear();
                foreach (var u in users) PublicUsers.Add(u);
                if (PublicUsers.Count == 1)
                    Username = PublicUsers[0].Name;
            });
        }
        catch { }

        _dispatcher.TryEnqueue(() => IsLoading = false);

        // Check Quick Connect availability in background
        _ = CheckQuickConnectEnabledAsync();
    }

    private async Task CheckQuickConnectEnabledAsync()
    {
        try
        {
            // Quick check: call QuickConnect init and see if it returns a secret
            var (result, _) = await _api.QuickConnectInitAsync();
            _dispatcher.TryEnqueue(() => IsQuickConnectEnabled = result != null);
        }
        catch
        {
            _dispatcher.TryEnqueue(() => IsQuickConnectEnabled = false);
        }
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            _dispatcher.TryEnqueue(() => ErrorMessage = "Enter a username.");
            return;
        }

        _dispatcher.TryEnqueue(() =>
        {
            ErrorMessage = null;
            IsLoading = true;
        });

        var (result, error) = await _api.AuthenticateAsync(_username, _password);

        _dispatcher.TryEnqueue(() =>
        {
            IsLoading = false;

            if (result == null)
            {
                ErrorMessage = error ?? "Login failed. Check your credentials.";
                return;
            }

            _password = string.Empty;
            OnPropertyChanged(nameof(Password));
            _nav.NavigateTo(typeof(HomePage));
        });
    }

    private void SelectUser(User? user)
    {
        if (user == null) return;
        _dispatcher.TryEnqueue(() => Username = user.Name);
    }

    private void OpenQuickConnect()
    {
        _nav.NavigateTo(typeof(QuickConnectPage));
    }
}
