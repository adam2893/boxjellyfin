using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage(SearchViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        SearchBox.Focus(FocusState.Keyboard);
        await ViewModel.LoadViewsAsync();
    }

    private void Result_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BaseItemDto item)
            ViewModel.NavigateToItemCommand.Execute(item);
    }

    private void View_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ViewItem view)
            ViewModel.NavigateToLibraryCommand.Execute(view);
    }
}
