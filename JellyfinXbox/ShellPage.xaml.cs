using System;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;

namespace JellyfinXbox;

public sealed partial class ShellPage : Page
{
    private readonly NavigationService _nav;
    private const string KeyServerUrl = "Jellyfin_ServerUrl";
    private const string KeyAccessToken = "Jellyfin_AccessToken";

    public ShellPage(NavigationService nav)
    {
        InitializeComponent();
        _nav = nav;
        _nav.Initialize(ContentFrame);

        // Handle Xbox B-button and system back button
        SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

        Loaded += OnLoaded;
    }

    /// <summary>
    /// Handles Xbox B-button (controller) and system back button.
    /// </summary>
    private void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        if (_nav.CanGoBack)
        {
            e.Handled = true;
            _nav.GoBack();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var api = App.GetService<JellyfinApiClient>();

        // Try restoring previous session (Wholphin-style)
        App.Log("[Shell] Attempting session restore...");
        var restored = await TryRestoreSessionAsync(api);

        if (!api.IsAuthenticated)
        {
            App.Log("[Shell] No valid session — showing ConnectPage");
            _nav.NavigateTo(typeof(ConnectPage));
        }
        else
        {
            App.Log(restored ? "[Shell] Session restored from LocalSettings" : "[Shell] Existing session still valid");
            HighlightNav("home");
            _nav.NavigateTo(typeof(HomePage));
        }
    }

    /// <summary>
    /// Saves server URL and access token for next launch.
    /// </summary>
    public static void SaveSession(JellyfinApiClient api)
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[KeyServerUrl] = api.ServerUrl ?? "";
            settings.Values[KeyAccessToken] = api.AccessToken ?? "";
        }
        catch { }
    }

    /// <summary>
    /// Attempts to restore a previous session. Returns true on success.
    /// </summary>
    private static async System.Threading.Tasks.Task<bool> TryRestoreSessionAsync(JellyfinApiClient api)
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            var savedUrl = settings.Values[KeyServerUrl] as string;
            var savedToken = settings.Values[KeyAccessToken] as string;

            if (string.IsNullOrEmpty(savedUrl) || string.IsNullOrEmpty(savedToken))
            {
                App.Log("[Shell] Restore: no saved credentials in LocalSettings");
                return false;
            }

            App.Log($"[Shell] Restore: found saved URL={savedUrl}, token={savedToken.Substring(0, Math.Min(8, savedToken.Length))}...");
            api.SetServerUrl(savedUrl);
            api.SetAccessToken(savedToken);

            if (!await api.ValidateTokenAsync())
            {
                App.LogWarn("[Shell] Restore: saved token is invalid/expired");
                ClearSession();
                api.Logout();
                return false;
            }

            // Load current user — required for IsAuthenticated
            try
            {
                var user = await api.GetCurrentUserAsync();
                api.CurrentUser = user;
                App.Log("[Shell] Restore: user loaded successfully");
            }
            catch (Exception ex)
            {
                App.LogWarn($"[Shell] Restore: failed to load user: {ex.Message}");
                ClearSession();
                api.Logout();
                return false;
            }

            App.Log("[Shell] Restore: token validated and user loaded");
            api.RaiseAuthChanged();
            return true;
        }
        catch (Exception ex)
        {
            App.LogWarn($"[Shell] Restore exception: {ex.GetType().Name}: {ex.Message}");
            ClearSession();
            return false;
        }
    }

    private static void ClearSession()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Remove(KeyServerUrl);
            settings.Values.Remove(KeyAccessToken);
        }
        catch { }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        var api = App.GetService<JellyfinApiClient>();
        var tag = btn.Tag?.ToString();

        // Require auth for Home and Search
        if (!api.IsAuthenticated && (tag == "home" || tag == "search"))
        {
            _nav.NavigateTo(typeof(ConnectPage));
            return;
        }

        HighlightNav(tag);
        switch (tag)
        {
            case "home": _nav.NavigateTo(typeof(HomePage)); break;
            case "search": _nav.NavigateTo(typeof(SearchPage)); break;
            case "quickconnect": _nav.NavigateTo(typeof(QuickConnectPage)); break;
            case "settings": _nav.NavigateTo(typeof(SettingsPage)); break;
        }
    }

    private void HighlightNav(string? tag)
    {
        var secondary = Application.Current.Resources["TextSecondaryBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;
        var accent = Application.Current.Resources["AccentBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;

        NavHome.Foreground = secondary;
        NavSearch.Foreground = secondary;
        NavQuickConnect.Foreground = secondary;
        NavSettings.Foreground = secondary;

        switch (tag)
        {
            case "home": NavHome.Foreground = accent; break;
            case "search": NavSearch.Foreground = accent; break;
            case "quickconnect": NavQuickConnect.Foreground = accent; break;
            case "settings": NavSettings.Foreground = accent; break;
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType != typeof(HomePage) &&
            e.SourcePageType != typeof(SearchPage) &&
            e.SourcePageType != typeof(QuickConnectPage) &&
            e.SourcePageType != typeof(SettingsPage))
            HighlightNav(null);
    }
}
