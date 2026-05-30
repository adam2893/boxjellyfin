using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;
using System.Linq;

namespace JellyfinXbox.ViewModels;

public class HomeViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;

    private bool _isLoading;
    private bool _hasContinueWatching;
    private bool _hasNextUp;

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasContinueWatching { get => _hasContinueWatching; set => SetProperty(ref _hasContinueWatching, value); }
    public bool HasNextUp { get => _hasNextUp; set => SetProperty(ref _hasNextUp, value); }

    public ObservableCollection<BaseItemDto> ContinueWatching { get; } = new();
    public ObservableCollection<BaseItemDto> NextUp { get; } = new();
    public ObservableCollection<BaseItemDto> LatestMovies { get; } = new();
    public ObservableCollection<BaseItemDto> LatestShows { get; } = new();
    public ObservableCollection<ViewItem> Views { get; } = new();

    public ICommand LoadDataCommand { get; }
    public ICommand NavigateToItemCommand { get; }
    public ICommand NavigateToLibraryCommand { get; }
    public ICommand NavigateToSearchCommand { get; }

    public HomeViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        NavigateToItemCommand = new RelayCommand<BaseItemDto>(NavigateToItem);
        NavigateToLibraryCommand = new RelayCommand<ViewItem>(NavigateToLibrary);
        NavigateToSearchCommand = new RelayCommand(NavigateToSearch);

        _api.OnAuthenticationChanged += () =>
        {
            if (!_api.IsAuthenticated)
                _nav.NavigateTo(typeof(LoginPage));
        };
    }

    public async Task LoadDataAsync()
    {
        if (!_api.IsAuthenticated) return;
        IsLoading = true;

        try
        {
            var resumeTask = _api.GetResumeItemsAsync(12);
            var nextUpTask = _api.GetNextUpAsync(limit: 12);
            var viewsTask = _api.GetViewsAsync();
            await Task.WhenAll(resumeTask, nextUpTask, viewsTask);

            ContinueWatching.Clear();
            foreach (var item in resumeTask.Result.Items) ContinueWatching.Add(item);
            HasContinueWatching = ContinueWatching.Count > 0;

            NextUp.Clear();
            foreach (var item in nextUpTask.Result.Items) NextUp.Add(item);
            HasNextUp = NextUp.Count > 0;

            var views = viewsTask.Result;
            Views.Clear();

            // Debug: log all views from server
            foreach (var v in views)
                System.Diagnostics.Debug.WriteLine($"[HomePage] View: Id={v.Id} Name={v.Name} Type={v.CollectionType}");

            // Exclude Live TV (not supported) and empty collection types
            foreach (var v in views.Where(v => v.CollectionType != "livetv"))
                Views.Add(v);

            LatestMovies.Clear();
            LatestShows.Clear();

            var movieLib = views.FirstOrDefault(v => v.CollectionType == "movies");
            var showLib = views.FirstOrDefault(v => v.CollectionType == "tvshows");

            var movieTask = movieLib != null
                ? _api.GetItemsAsync(movieLib.Id, sortBy: "DateCreated", sortOrder: "Descending", limit: 12, includeItemTypes: "Movie")
                : Task.FromResult(new ItemsResult());
            var showTask = showLib != null
                ? _api.GetItemsAsync(showLib.Id, sortBy: "DateCreated", sortOrder: "Descending", limit: 12, includeItemTypes: "Series")
                : Task.FromResult(new ItemsResult());
            await Task.WhenAll(movieTask, showTask);

            foreach (var item in movieTask.Result.Items) LatestMovies.Add(item);
            foreach (var item in showTask.Result.Items) LatestShows.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomePage] LoadDataAsync failed: {ex}");
        }

        IsLoading = false;
    }

    private void NavigateToItem(BaseItemDto? item)
    {
        if (item != null) _nav.NavigateTo(typeof(MediaDetailPage), item.Id);
    }

    private void NavigateToLibrary(ViewItem? view)
    {
        if (view != null) _nav.NavigateTo(typeof(LibraryPage), (view.Id, view.Name));
    }

    private void NavigateToSearch() => _nav.NavigateTo(typeof(SearchPage));
}
