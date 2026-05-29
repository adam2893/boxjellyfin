using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;
using DispatcherQueue = Windows.System.DispatcherQueue;

namespace JellyfinXbox.ViewModels;

public class QuickConnectViewModel : ObservableObject, IDisposable
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _pollCts;

    private bool _isActive;
    private string _code = "";
    private string _statusMessage = "Press Start to begin Quick Connect";
    private bool _isSuccess;
    private bool _isError;
    private string _errorMessage = "";

    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }
    public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand GoBackCommand { get; }

    public QuickConnectViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        StartCommand = new AsyncRelayCommand(StartQuickConnectAsync);
        CancelCommand = new RelayCommand(Cancel);
        GoBackCommand = new RelayCommand(GoBack);
    }

    private async Task StartQuickConnectAsync()
    {
        _dispatcher.TryEnqueue(() =>
        {
            IsActive = true;
            IsSuccess = false;
            IsError = false;
            ErrorMessage = "";
        });

        // 1. Initiate Quick Connect — POST /QuickConnect/Initiate
        var (initResult, error) = await _api.QuickConnectInitAsync();

        if (initResult == null)
        {
            _dispatcher.TryEnqueue(() =>
            {
                IsError = true;
                ErrorMessage = error ?? "Quick Connect unavailable. Check server settings.";
                IsActive = false;
            });
            return;
        }

        // Start polling on a background task
        _pollCts = new CancellationTokenSource();
        var secret = initResult.Secret;
        var code = initResult.Code;
        var ct = _pollCts.Token;

        _dispatcher.TryEnqueue(() =>
        {
            Code = code;
            StatusMessage = $"Enter this code on another device: {code}";
            Debug.WriteLine($"[QuickConnect] Init OK — Code: {code}");
        });

        // Run polling on thread pool
        _ = Task.Run(() => PollForAuthorization(secret, ct));
    }

    private async Task PollForAuthorization(string secret, CancellationToken ct)
    {
        const int maxAttempts = 120; // 2 minutes
        for (int i = 0; i < maxAttempts && !ct.IsCancellationRequested; i++)
        {
            try { await Task.Delay(1000, ct); }
            catch (OperationCanceledException) { return; }

            var status = await _api.QuickConnectCheckAsync(secret);
            if (status == null) continue;

            Debug.WriteLine($"[QuickConnect] Poll {i + 1}: Authenticated={status.Authenticated}");

            if (status.Authenticated)
            {
                // If the Connect response already contains tokens, use them directly
                if (status.Authentication?.AccessToken != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        _api.AccessToken = status.Authentication.AccessToken;
                        _api.CurrentUser = status.Authentication.User;
                        _api.ApplyAuth();
                        ShellPage.SaveSession(_api);
                        _api.RaiseAuthChanged();
                        IsSuccess = true;
                        StatusMessage = $"Connected as {status.Authentication.User.Name}!";
                    });
                    await Task.Delay(1500, CancellationToken.None);
                    _dispatcher.TryEnqueue(() => _nav.NavigateTo(typeof(HomePage)));
                    return;
                }

                // Otherwise exchange secret for auth tokens — POST /Users/AuthenticateWithQuickConnect
                _dispatcher.TryEnqueue(() => StatusMessage = "Authorized! Signing in...");

                var (authResult, authError) = await _api.QuickConnectAuthenticateAsync(secret);

                if (authResult != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        ShellPage.SaveSession(_api);
                        IsSuccess = true;
                        StatusMessage = $"Connected as {authResult.User.Name}!";
                    });
                    await Task.Delay(1500, CancellationToken.None);
                    _dispatcher.TryEnqueue(() => _nav.NavigateTo(typeof(HomePage)));
                }
                else
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        IsError = true;
                        ErrorMessage = authError ?? "Token exchange failed.";
                        StatusMessage = "Login failed.";
                    });
                }
                return;
            }
        }

        if (!ct.IsCancellationRequested)
        {
            _dispatcher.TryEnqueue(() =>
            {
                IsError = true;
                ErrorMessage = "No device responded within 2 minutes.";
                StatusMessage = "Timed out.";
            });
        }
    }

    private void Cancel()
    {
        _pollCts?.Cancel();
        _dispatcher.TryEnqueue(() =>
        {
            IsActive = false;
            Code = "";
            StatusMessage = "Press Start to begin Quick Connect";
        });
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
