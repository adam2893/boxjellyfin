using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using JellyfinClient.Models;
using JellyfinClient.Services;
using JellyfinXbox.Services;
using JellyfinXbox.Views;
using System.Linq;

namespace JellyfinXbox.ViewModels;

public class MediaDetailViewModel : ObservableObject
{
    private readonly JellyfinApiClient _api;
    private readonly NavigationService _nav;

    private bool _isLoading;
    private bool _isSeries;
    private BaseItemDto? _item;
    private string _title = "";
    private string _subtitle = "";
    private string? _description;
    private string? _backdropUrl;
    private string? _posterUrl;
    private string? _communityRating;
    private string? _runtime;
    private string? _officialRating;
    private int _selectedSeasonIndex;

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool IsSeries { get => _isSeries; set => SetProperty(ref _isSeries, value); }
    public BaseItemDto? Item { get => _item; set => SetProperty(ref _item, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Subtitle { get => _subtitle; set => SetProperty(ref _subtitle, value); }
    public string? Description { get => _description; set => SetProperty(ref _description, value); }
    public string? BackdropUrl { get => _backdropUrl; set => SetProperty(ref _backdropUrl, value); }
    public string? PosterUrl { get => _posterUrl; set => SetProperty(ref _posterUrl, value); }
    public string? CommunityRating { get => _communityRating; set => SetProperty(ref _communityRating, value); }
    public string? Runtime { get => _runtime; set => SetProperty(ref _runtime, value); }
    public string? OfficialRating { get => _officialRating; set => SetProperty(ref _officialRating, value); }

    public int SelectedSeasonIndex
    {
        get => _selectedSeasonIndex;
        set
        {
            if (SetProperty(ref _selectedSeasonIndex, value))
                OnSelectedSeasonIndexChanged(value);
        }
    }

    public ObservableCollection<BaseItemDto> Seasons { get; } = new();
    public ObservableCollection<BaseItemDto> Episodes { get; } = new();
    public ObservableCollection<BaseItemPerson> Cast { get; } = new();
    public ObservableCollection<string> Genres { get; } = new();

    public ICommand NavigateToItemCommand { get; }
    public ICommand PlayItemCommand { get; }
    public ICommand GoBackCommand { get; }

    public MediaDetailViewModel(JellyfinApiClient api, NavigationService nav)
    {
        _api = api;
        _nav = nav;
        NavigateToItemCommand = new RelayCommand<BaseItemDto>(NavigateToItem);
        PlayItemCommand = new RelayCommand<BaseItemDto>(PlayItem);
        GoBackCommand = new RelayCommand(GoBack);
    }

    public async Task LoadAsync(string itemId)
    {
        IsLoading = true;

        var item = await _api.GetItemAsync(itemId);
        if (item == null)
        {
            IsLoading = false;
            return;
        }

        Item = item;
        Title = item.Name;
        Description = item.Overview;

        if (item.ProductionYear.HasValue)
            Subtitle = item.ProductionYear.Value.ToString();
        if (!string.IsNullOrEmpty(item.OfficialRating))
            OfficialRating = item.OfficialRating;

        Runtime = _api.FormatRuntime(_api.GetRuntime(item));
        CommunityRating = item.CommunityRating.HasValue
            ? $"★ {item.CommunityRating.Value:F1}"
            : null;

        PosterUrl = _api.GetImageUrl(item.Id, "Primary");
        var backdropId = item.BackdropImageTags.Count > 0 ? item.Id : item.ParentBackdropItemId;
        if (!string.IsNullOrEmpty(backdropId))
            BackdropUrl = _api.GetBackdropUrl(backdropId, maxWidth: 1920);

        Genres.Clear();
        foreach (var g in item.Genres) Genres.Add(g);

        Cast.Clear();
        foreach (var p in item.People.Where(p => p.Type == "Actor").Take(12))
            Cast.Add(p);

        if (item.Type == "Series")
        {
            IsSeries = true;
            await LoadSeasonsAsync(item.Id);
        }
        else if (item.Type == "Episode" && !string.IsNullOrEmpty(item.SeriesId))
        {
            Subtitle = $"S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2}";
            if (!string.IsNullOrEmpty(item.SeriesName))
                Title = item.SeriesName;
        }

        IsLoading = false;
    }

    private async Task LoadSeasonsAsync(string seriesId)
    {
        var result = await _api.GetSeasonsAsync(seriesId);
        Seasons.Clear();
        foreach (var s in result.Items) Seasons.Add(s);
        if (Seasons.Count > 0)
            SelectedSeasonIndex = 0;
    }

    private void OnSelectedSeasonIndexChanged(int value)
    {
        if (Seasons.Count > value && Item != null)
            _ = LoadEpisodesAsync(Item.Id, Seasons[value].Id);
    }

    private async Task LoadEpisodesAsync(string seriesId, string seasonId)
    {
        Episodes.Clear();
        var result = await _api.GetEpisodesAsync(seriesId, seasonId);
        foreach (var ep in result.Items) Episodes.Add(ep);
    }

    private void NavigateToItem(BaseItemDto? item)
    {
        if (item != null) _nav.NavigateTo(typeof(MediaDetailPage), item.Id);
    }

    private void PlayItem(BaseItemDto? item)
    {
        if (item != null) _nav.NavigateTo(typeof(PlayerPage), item.Id);
    }

    private void GoBack() => _nav.GoBack();
}
