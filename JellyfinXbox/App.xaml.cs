using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Activation;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.ViewModels;
using JellyfinXbox.Views;

namespace JellyfinXbox;

public partial class App : Application
{
    private static readonly Dictionary<Type, object> _services = new();
    private static readonly object _logLock = new();
    private static string? _logPath;
    private static readonly Dictionary<Type, Func<object>> _factories = new();

    public static T GetService<T>() where T : class => (T)_services[typeof(T)];

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
        RegisterServices();
    }

    private static void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[CRASH] {e.Exception.GetType().Name}: {e.Exception.Message}");
        System.Diagnostics.Debug.WriteLine($"[CRASH] Stack: {e.Exception.StackTrace}");
        e.Handled = true; // Prevent silent kill — app will stay alive so we can see error
    }

    private static void RegisterServices()
    {
        // Core services
        var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
        });
        var api = new JellyfinApiClient(http);
        var nav = new NavigationService();

        _services[typeof(HttpClient)] = http;
        _services[typeof(JellyfinApiClient)] = api;
        _services[typeof(NavigationService)] = nav;

        // Register factory delegates for transient services (created on demand)
        _factories[typeof(ConnectViewModel)] = () => new ConnectViewModel(api, nav);
        _factories[typeof(LoginViewModel)] = () => new LoginViewModel(api, nav);
        _factories[typeof(HomeViewModel)] = () => new HomeViewModel(api, nav);
        _factories[typeof(MediaDetailViewModel)] = () => new MediaDetailViewModel(api, nav);
        _factories[typeof(PlayerViewModel)] = () => new PlayerViewModel(api, nav);
        _factories[typeof(SearchViewModel)] = () => new SearchViewModel(api, nav);
        _factories[typeof(ShellViewModel)] = () => new ShellViewModel(nav);
        _factories[typeof(SettingsViewModel)] = () => new SettingsViewModel(api, nav);
        _factories[typeof(QuickConnectViewModel)] = () => new QuickConnectViewModel(api, nav);
        _factories[typeof(LibraryViewModel)] = () => new LibraryViewModel(api, nav);

        // Register pages (created on demand, same as transient)
        _factories[typeof(ShellPage)] = () => new ShellPage(nav);
        _factories[typeof(ConnectPage)] = () => new ConnectPage(GetViewModel<ConnectViewModel>());
        _factories[typeof(LoginPage)] = () => new LoginPage(GetViewModel<LoginViewModel>());
        _factories[typeof(HomePage)] = () => new HomePage(GetViewModel<HomeViewModel>());
        _factories[typeof(MediaDetailPage)] = () => new MediaDetailPage(GetViewModel<MediaDetailViewModel>());
        _factories[typeof(PlayerPage)] = () => new PlayerPage(GetViewModel<PlayerViewModel>());
        _factories[typeof(SearchPage)] = () => new SearchPage(GetViewModel<SearchViewModel>(), GetService<NavigationService>());
        _factories[typeof(SettingsPage)] = () => new SettingsPage(GetViewModel<SettingsViewModel>());
        _factories[typeof(QuickConnectPage)] = () => new QuickConnectPage(GetViewModel<QuickConnectViewModel>());
        _factories[typeof(LibraryPage)] = () => new LibraryPage(nav, GetViewModel<LibraryViewModel>());
    }

    public static T Create<T>() where T : class
    {
        if (_factories.TryGetValue(typeof(T), out var factory))
            return (T)factory();
        throw new InvalidOperationException($"No factory registered for {typeof(T).Name}");
    }

    public static object Create(Type type)
    {
        if (_factories.TryGetValue(type, out var factory))
            return factory();
        throw new InvalidOperationException($"No factory registered for {type.Name}");
    }

    private static T GetViewModel<T>() where T : class
    {
        return Create<T>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // File-based logging (Debug.WriteLine is invisible on Xbox)
        InitLogging();

        // Xbox TV-safe area / full-screen setup
        var view = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
        view.SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);

        var shell = Create<ShellPage>();
        Window.Current.Content = shell;
        Window.Current.Activate();
    }

    // ═══════════════════════════════════════════════════════════════
    // File-based logging (Xbox can't see Debug output)
    // Log file: ApplicationData.LocalFolder\jellyfinxbox.log
    // ═══════════════════════════════════════════════════════════════
    private static void InitLogging()
    {
        try
        {
            _logPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "jellyfinxbox.log");
            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.WriteAllText(_logPath, $"[{ts}] JellyfinXbox v1.0.6.1 started{Environment.NewLine}");
        }
        catch { _logPath = null; }
    }

    public static void Log(string message)
    {
        try
        {
            if (_logPath == null) return;
            var ts = DateTime.Now.ToString("HH:mm:ss");
            lock (_logLock)
                File.AppendAllText(_logPath, $"[{ts}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    public static void LogWarn(string message)
    {
        try
        {
            if (_logPath == null) return;
            var ts = DateTime.Now.ToString("HH:mm:ss");
            lock (_logLock)
                File.AppendAllText(_logPath, $"[{ts}] WARN {message}{Environment.NewLine}");
        }
        catch { }
    }

    public static string? LogPath => _logPath;
}
