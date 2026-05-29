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

        // Manage visibility of overlapping panels (all in Grid.Row="2")
        ViewModel.PropertyChanged += (s, e) =>
        {
            var dq = Windows.System.DispatcherQueue.GetForCurrentThread();
            dq.TryEnqueue(() =>
            {
                if (ViewModel.IsSearching)
                {
                    LoadingRing.Visibility = Visibility.Visible;
                    ViewsPanel.Visibility = Visibility.Collapsed;
                    ResultsList.Visibility = Visibility.Collapsed;
                }
                else if (ViewModel.HasResults)
                {
                    LoadingRing.Visibility = Visibility.Collapsed;
                    ViewsPanel.Visibility = Visibility.Collapsed;
                    ResultsList.Visibility = Visibility.Visible;
                }
                else
                {
                    LoadingRing.Visibility = Visibility.Collapsed;
                    ViewsPanel.Visibility = ViewModel.ShowViews ? Visibility.Visible : Visibility.Collapsed;
                    ResultsList.Visibility = Visibility.Collapsed;
                }
            });
        };
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
