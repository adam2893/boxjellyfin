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
        Loaded -= OnLoaded;
        await ViewModel.LoadDataAsync();
    }

    private void MediaItem_Click(object sender, ItemClickEventArgs e)
    {
        App.Log($"[Click] HomePage item: {(e.ClickedItem as BaseItemDto)?.Name ?? e.ClickedItem?.GetType().Name ?? "null"}");
        if (e.ClickedItem is BaseItemDto item)
            ViewModel.NavigateToItemCommand.Execute(item);
    }

    private void Library_Click(object sender, ItemClickEventArgs e)
    {
        App.Log($"[Click] Library: {(e.ClickedItem as ViewItem)?.Name ?? e.ClickedItem?.GetType().Name ?? "null"}");
        if (e.ClickedItem is ViewItem view)
            ViewModel.NavigateToLibraryCommand.Execute(view);
    }
}
