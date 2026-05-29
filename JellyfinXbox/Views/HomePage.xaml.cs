using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // OnNavigatedTo doesn't fire with NavigationService (uses frame.Content = page).
        // Loaded event is the reliable trigger for our service-locator navigation.
        Loaded -= OnLoaded; // Only fire once
        await ViewModel.LoadDataAsync();
    }

    private void MediaItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BaseItemDto item)
            ViewModel.NavigateToItemCommand.Execute(item);
    }
}
