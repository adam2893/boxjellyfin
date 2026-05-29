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

        if (!api.IsAuthenticated)
            _nav.NavigateTo(typeof(ConnectPage));
        else
        {
            HighlightNav("home");
            _nav.NavigateTo(typeof(HomePage));
        }
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
