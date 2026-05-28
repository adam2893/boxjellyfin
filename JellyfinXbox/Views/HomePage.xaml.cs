using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;
using System;
using System.Threading.Tasks;

namespace JellyfinXbox.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = LoadDataSafeAsync();
    }

    private async Task LoadDataSafeAsync()
    {
        try { await ((AsyncRelayCommand)ViewModel.LoadDataCommand).ExecuteAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"HomePage load failed: {ex.Message}"); }
    }

    private void MediaItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BaseItemDto item)
            ViewModel.NavigateToItemCommand.Execute(item);
    }
}