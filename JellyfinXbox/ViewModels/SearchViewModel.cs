using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;

namespace JellyfinXbox.ViewModels;

public class SearchViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;

    private string _query = "";
    private bool _isSearching;
    private bool _hasResults;
    private bool _showViews = true;

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
                OnQueryChanged(value);
        }
    }

    public bool IsSearching { get => _isSearching; set => SetProperty(ref _isSearching, value); }
    public bool HasResults { get => _hasResults; set => SetProperty(ref _hasResults, value); }
    public bool ShowViews { get => _showViews; set => SetProperty(ref _showViews, value); }

    public ObservableCollection<BaseItemDto> Results { get; } = new();
    public ObservableCollection<ViewItem> Views { get; } = new();

    public ICommand SearchCommand { get; }
    public ICommand NavigateToItemCommand { get; }
    public ICommand NavigateToLibraryCommand { get; }

    public SearchViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        SearchCommand = new AsyncRelayCommand<string>(SearchAsync);
        NavigateToItemCommand = new RelayCommand<BaseItemDto>(NavigateToItem);
        NavigateToLibraryCommand = new RelayCommand<ViewItem>(NavigateToLibrary);
    }

    public async Task LoadViewsAsync()
    {
        var views = await _api.GetViewsAsync();
        Views.Clear();
        foreach (var v in views) Views.Add(v);
    }

    private void OnQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Results.Clear();
            HasResults = false;
            ShowViews = true;
            return;
        }

        if (value.Length >= 2)
        {
            ShowViews = false;
            SearchCommand.Execute(value);
        }
    }

    private async Task SearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        IsSearching = true;
        ShowViews = false;
        HasResults = false;
        var result = await _api.SearchAsync(query);
        Results.Clear();
        if (result.Items != null)
        {
            foreach (var item in result.Items) Results.Add(item);
        }
        HasResults = Results.Count > 0;
        IsSearching = false;
    }

    private void NavigateToItem(BaseItemDto? item)
    {
        if (item != null) _nav.NavigateTo(typeof(MediaDetailPage), item.Id);
    }

    private void NavigateToLibrary(ViewItem? view)
    {
        if (view != null) _nav.NavigateTo(typeof(LibraryPage), (view.Id, view.Name));
    }
}
