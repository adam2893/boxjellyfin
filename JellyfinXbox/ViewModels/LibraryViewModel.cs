using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;

namespace JellyfinXbox.ViewModels;

public class LibraryViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;
    private string _currentLibraryId = "";

    private string _libraryName = "";
    private bool _isLoading;

    public string LibraryName { get => _libraryName; set => SetProperty(ref _libraryName, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public ObservableCollection<BaseItemDto> Items { get; } = new();

    public ICommand LoadCommand { get; }
    public ICommand NavigateToItemCommand { get; }
    public AsyncRelayCommand<string> LoadAsyncCommand { get; }

    public LibraryViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        LoadAsyncCommand = new AsyncRelayCommand<string>(LoadAsync);
        LoadCommand = LoadAsyncCommand;
        NavigateToItemCommand = new RelayCommand<BaseItemDto>(NavigateToItem);
    }

    private async Task LoadAsync(string? libraryId)
    {
        if (libraryId == null) return;
        _currentLibraryId = libraryId;
        await RefreshAsync("DateCreated", "Descending");
    }

    public async Task SortAsync(string sortBy, string sortOrder)
    {
        await RefreshAsync(sortBy, sortOrder);
    }

    private async Task RefreshAsync(string sortBy, string sortOrder)
    {
        if (string.IsNullOrEmpty(_currentLibraryId)) return;
        IsLoading = true;
        try
        {
            var result = await _api.GetItemsAsync(_currentLibraryId, sortBy: sortBy, sortOrder: sortOrder, limit: 50);
            Items.Clear();
            foreach (var item in result.Items) Items.Add(item);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Library refresh failed: {ex.Message}"); }
        IsLoading = false;
    }

    private void NavigateToItem(BaseItemDto? item)
    {
        if (item != null) _nav.NavigateTo(typeof(MediaDetailPage), item.Id);
    }
}
