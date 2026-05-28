using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;

namespace JellyfinXbox.ViewModels;

public class QuickConnectViewModel : ObservableObject, IDisposable
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private CancellationTokenSource? _pollCts;

    private bool _isActive;
    private string _code = "";
    private string _statusMessage = "Waiting for connection...";
    private bool _isSuccess;
    private bool _isError;
    private string _errorMessage = "";
    private string _connectedUserName = "";

    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }
    public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public string ConnectedUserName { get => _connectedUserName; set => SetProperty(ref _connectedUserName, value); }

    public ICommand StartQuickConnectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand GoBackCommand { get; }

    public QuickConnectViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        StartQuickConnectCommand = new AsyncRelayCommand(StartQuickConnectAsync);
        CancelCommand = new RelayCommand(Cancel);
        GoBackCommand = new RelayCommand(GoBack);
    }

    private async Task StartQuickConnectAsync()
    {
        IsActive = true;
        IsSuccess = false;
        IsError = false;
        ErrorMessage = "";
        ConnectedUserName = "";

        var initResult = await _api.QuickConnectInitAsync();
        if (initResult == null)
        {
            IsError = true;
            ErrorMessage = "Quick Connect is not available on this server. Enable it in Jellyfin server settings.";
            IsActive = false;
            return;
        }

        Code = initResult.Code;
        StatusMessage = $"Enter code {Code} on another device";
        Debug.WriteLine($"[QuickConnect] Initiated with code: {Code}");

        _pollCts = new CancellationTokenSource();
        try
        {
            await PollForAuthorization(initResult.Secret, _pollCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (IsActive)
                StatusMessage = "Connection timed out. Try again.";
        }
    }

    private async Task PollForAuthorization(string secret, CancellationToken ct)
    {
        int attempts = 0;
        const int maxAttempts = 120;

        while (!ct.IsCancellationRequested && attempts < maxAttempts)
        {
            await Task.Delay(1000, ct);
            attempts++;

            var authResult = await _api.QuickConnectCheckAuthAsync(secret);
            if (authResult == null) continue;

            if (authResult.Authorized)
            {
                StatusMessage = $"Authorized! Connecting as {authResult.UserName}...";

                var connectResult = await _api.QuickConnectConnectAsync(secret);
                if (connectResult != null)
                {
                    IsSuccess = true;
                    ConnectedUserName = connectResult.User.Name;
                    StatusMessage = $"Connected as {ConnectedUserName}!";
                    Debug.WriteLine($"[QuickConnect] Connected as {ConnectedUserName}");

                    await Task.Delay(1500, CancellationToken.None);
                    _nav.NavigateTo(typeof(HomePage));
                }
                else
                {
                    IsError = true;
                    ErrorMessage = "Authorization succeeded but connection failed. Try again.";
                }
                return;
            }
        }

        if (IsActive)
        {
            IsError = true;
            StatusMessage = "Connection timed out.";
            ErrorMessage = "No device responded within 2 minutes. Try again.";
        }
    }

    private void Cancel()
    {
        _pollCts?.Cancel();
        IsActive = false;
        Code = "";
        StatusMessage = "Waiting for connection...";
    }

    private void GoBack()
    {
        _pollCts?.Cancel();
        _nav.GoBack();
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
    }
}
