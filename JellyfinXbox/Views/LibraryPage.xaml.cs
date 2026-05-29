using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinClient.Models;
using JellyfinXbox.Services;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class LibraryPage : Page
{
    private readonly NavigationService _nav;
    public LibraryViewModel ViewModel { get; }

    private string? _pendingLibraryId;
    private string? _pendingLibraryName;

    public LibraryPage(NavigationService nav, LibraryViewModel viewModel)
    {
        _nav = nav;
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// NavigationService sets _frame.Content directly, bypassing OnNavigatedTo.
    /// Instead, we pass parameters and trigger loading via the Loaded event.
    /// </summary>
    public void Initialize(string libraryId, string libraryName)
    {
        _pendingLibraryId = libraryId;
        _pendingLibraryName = libraryName;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_pendingLibraryId != null)
        {
            ViewModel.LibraryName = _pendingLibraryName ?? "";
            try
            {
                await ViewModel.LoadAsyncCommand.ExecuteAsync(_pendingLibraryId);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Library load failed: {ex.Message}"); }
        }
    }

    private void Item_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BaseItemDto item)
            ViewModel.NavigateToItemCommand.Execute(item);
    }

    private void SortNewest_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("DateCreated", "Descending"); }
    private void SortOldest_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("DateCreated", "Ascending"); }
    private void SortTitle_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("SortName", "Ascending"); }
    private void SortRating_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("CommunityRating", "Descending"); }
    private void Back_Click(object sender, RoutedEventArgs e) { _nav.GoBack(); }
    private void FilterAll_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("SortName", "Ascending"); }
    private void FilterUnwatched_Click(object sender, RoutedEventArgs e) { }
    private void FilterFavorites_Click(object sender, RoutedEventArgs e) { }
}
