using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using JellyfinClient.Models;
using JellyfinXbox.Services;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class LibraryPage : Page
{
    private readonly NavigationService _nav;
    public LibraryViewModel ViewModel { get; }

    public LibraryPage(NavigationService nav, LibraryViewModel viewModel)
    {
        _nav = nav;
        ViewModel = viewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ValueTuple<string, string> tuple)
        {
            ViewModel.LibraryName = tuple.Item2;
            _ = LoadSafeAsync(tuple.Item1);
        }
    }

    private async Task LoadSafeAsync(string id)
    {
        try { await ((AsyncRelayCommand<string>)ViewModel.LoadCommand).ExecuteAsync(id); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Library load failed: {ex.Message}"); }
    }

    private void Item_Click(object sender, ItemClickEventArgs e) { if (e.ClickedItem is BaseItemDto item) ViewModel.NavigateToItemCommand.Execute(item); }
    private void SortNewest_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("DateCreated", "Descending"); }
    private void SortOldest_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("DateCreated", "Ascending"); }
    private void SortTitle_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("SortName", "Ascending"); }
    private void SortRating_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("CommunityRating", "Descending"); }
    private void Back_Click(object sender, RoutedEventArgs e) { _nav.GoBack(); }
    private void FilterAll_Click(object sender, RoutedEventArgs e) { _ = ViewModel.SortAsync("SortName", "Ascending"); }
    private void FilterUnwatched_Click(object sender, RoutedEventArgs e) { }
    private void FilterFavorites_Click(object sender, RoutedEventArgs e) { }
}