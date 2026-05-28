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

    public ShellPage(NavigationService nav)
    {
        InitializeComponent();
        _nav = nav;
        _nav.Initialize(ContentFrame);

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var api = App.GetService<JellyfinApiClient>();

        // Set initial focus for gamepad
        NavHome.Focus(FocusState.Programmatic);

        if (!api.IsAuthenticated)
        {
            _nav.NavigateTo(typeof(LoginPage));
        }
        else
        {
            HighlightNav("home");
            _nav.NavigateTo(typeof(HomePage));
        }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var tag = btn.Tag?.ToString();
            HighlightNav(tag);
            switch (tag)
            {
                case "home":
                    _nav.NavigateTo(typeof(HomePage));
                    break;
                case "search":
                    _nav.NavigateTo(typeof(SearchPage));
                    break;
                case "quickconnect":
                    _nav.NavigateTo(typeof(QuickConnectPage));
                    break;
                case "settings":
                    _nav.NavigateTo(typeof(SettingsPage));
                    break;
            }
        }
    }

    private void HighlightNav(string? tag)
    {
        // Reset all
        NavHome.Foreground = Application.Current.Resources["TextSecondaryBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;
        NavSearch.Foreground = Application.Current.Resources["TextSecondaryBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;
        NavQuickConnect.Foreground = Application.Current.Resources["TextSecondaryBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;
        NavSettings.Foreground = Application.Current.Resources["TextSecondaryBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;

        // Highlight active
        var accentBrush = Application.Current.Resources["AccentBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;
        switch (tag)
        {
            case "home": NavHome.Foreground = accentBrush; break;
            case "search": NavSearch.Foreground = accentBrush; break;
            case "quickconnect": NavQuickConnect.Foreground = accentBrush; break;
            case "settings": NavSettings.Foreground = accentBrush; break;
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType != typeof(HomePage) &&
            e.SourcePageType != typeof(SearchPage) &&
            e.SourcePageType != typeof(QuickConnectPage) &&
            e.SourcePageType != typeof(SettingsPage))
        {
            HighlightNav(null);
        }
    }
}
