using System;
using Windows.UI.Xaml.Controls;
using JellyfinXbox.Views;

namespace JellyfinXbox.Services;

public class NavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame) => _frame = frame;

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null) return;

        // Pages have constructor dependencies — create via service locator, not Frame.Navigate
        var page = App.Create(pageType) as Page;
        if (page == null) return;

        // Pass parameters to pages that need them (OnNavigatedTo doesn't fire with frame.Content)
        if (page is LibraryPage libPage && parameter is ValueTuple<string, string> libTuple)
            libPage.Initialize(libTuple.Item1, libTuple.Item2);
        else if (page is MediaDetailPage detailPage && parameter is string itemId)
            detailPage.Initialize(itemId);
        else if (page is PlayerPage playerPage && parameter is string playerItemId)
            playerPage.Initialize(playerItemId);

        _frame.Content = page;
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;
}
