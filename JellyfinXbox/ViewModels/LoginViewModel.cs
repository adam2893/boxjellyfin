using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;

namespace JellyfinXbox.ViewModels;

public class LoginViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;

    private string _serverUrl = "";
    private string _username = "";
    private string _password = "";
    private bool _isLoading;
    private string? _errorMessage;
    private bool _showPasswordField;
    private string _serverName = "";

    public string ServerUrl { get => _serverUrl; set => SetProperty(ref _serverUrl, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public bool ShowPasswordField { get => _showPasswordField; set => SetProperty(ref _showPasswordField, value); }
    public string ServerName { get => _serverName; set => SetProperty(ref _serverName, value); }

    public ObservableCollection<User> PublicUsers { get; } = new();

    public ICommand ConnectCommand { get; }
    public ICommand LoginCommand { get; }
    public ICommand SelectUserCommand { get; }
    public ICommand QuickConnectCommand { get; }

    public LoginViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        SelectUserCommand = new RelayCommand<User>(SelectUser);
        QuickConnectCommand = new RelayCommand(OpenQuickConnect);
    }

    private async Task ConnectAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            ErrorMessage = "Enter a server URL.";
            return;
        }

        if (!ServerUrl.StartsWith("http"))
            ServerUrl = "https://" + ServerUrl;

        IsLoading = true;
        _api.SetServerUrl(ServerUrl);

        var info = await _api.GetServerInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Cannot connect to server. Check the URL and try again.";
            IsLoading = false;
            return;
        }

        ServerName = info.ServerName;

        // Always show login fields after connecting — even if no public users
        ShowPasswordField = true;

        var users = await _api.GetPublicUsersAsync();
        PublicUsers.Clear();
        foreach (var u in users) PublicUsers.Add(u);

        if (PublicUsers.Count == 1)
            Username = PublicUsers[0].Name;

        IsLoading = false;
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Enter a username.";
            return;
        }

        ErrorMessage = null;
        IsLoading = true;

        var result = await _api.AuthenticateAsync(Username, Password);
        if (result == null)
        {
            ErrorMessage = "Login failed. Check your credentials.";
            IsLoading = false;
            return;
        }

        IsLoading = false;
        Password = string.Empty;
        _nav.NavigateTo(typeof(Views.HomePage));
    }

    private void SelectUser(User? user)
    {
        if (user == null) return;
        Username = user.Name;
    }

    private void OpenQuickConnect()
    {
        _nav.NavigateTo(typeof(QuickConnectPage));
    }
}
