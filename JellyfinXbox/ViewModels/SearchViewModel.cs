using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;
using System.Linq;

namespace JellyfinXbox.ViewModels;

public class SearchViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;

    private string _query = "";
    private bool _isSearching;
    private bool _hasResults;

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

    public ObservableCollection<BaseItemDto> Results { get; } = new();

    public ICommand SearchCommand { get; }
    public ICommand NavigateToItemCommand { get; }

    public SearchViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        SearchCommand = new AsyncRelayCommand<string>(SearchAsync);
        NavigateToItemCommand = new RelayCommand<BaseItemDto>(NavigateToItem);
    }

    private void OnQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            Results.Clear();
            HasResults = false;
            return;
        }
        SearchCommand.Execute(value);
    }

    private async Task SearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        IsSearching = true;
        var result = await _api.SearchAsync(query);
        Results.Clear();
        foreach (var item in result.Items) Results.Add(item);
        HasResults = Results.Count > 0;
        IsSearching = false;
    }

    private void NavigateToItem(BaseItemDto? item)
    {
        if (item != null) _nav.NavigateTo(typeof(MediaDetailPage), item.Id);
    }
}
