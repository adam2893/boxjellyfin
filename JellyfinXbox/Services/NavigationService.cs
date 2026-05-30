using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinXbox.Views;

namespace JellyfinXbox.Services;

/// <summary>
/// Navigation service with manual back stack and proper focus management.
/// Pages use constructor DI via App service locator — so we can't use Frame.Navigate()
/// directly. Instead, we create pages manually, maintain our own back stack, and
/// ensure focus is properly transferred.
/// </summary>
public class NavigationService
{
    private Frame? _frame;
    private readonly Stack<(Type PageType, object? Parameter)> _backStack = new();
    private Type? _currentPageType;

    public void Initialize(Frame frame) => _frame = frame;

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null) return;

        // Save current page to back stack before navigating
        if (_currentPageType != null && _currentPageType != pageType)
            _backStack.Push((_currentPageType, null));

        _currentPageType = pageType;

        var page = App.Create(pageType) as Page;
        if (page == null) return;

        // Pass parameters to pages that need them
        if (page is LibraryPage libPage && parameter is ValueTuple<string, string> libTuple)
            libPage.Initialize(libTuple.Item1, libTuple.Item2);
        else if (page is MediaDetailPage detailPage && parameter is string itemId)
            detailPage.Initialize(itemId);
        else if (page is PlayerPage playerPage && parameter is string playerItemId)
            playerPage.Initialize(playerItemId);

        _frame.Content = page;

        // Restore focus to the new page so gamepad / click events work
        _frame.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Navigates back to previous page using manual back stack.
    /// Also used for "Back" navigation from the home button.
    /// </summary>
    public void GoBack()
    {
        if (_backStack.Count == 0) return;

        var (pageType, parameter) = _backStack.Pop();
        _currentPageType = null; // prevent re-pushing when NavigateTo saves

        NavigateTo(pageType, parameter);
    }

    /// <summary>
    /// Returns true if there's a page to go back to.
    /// </summary>
    public bool CanGoBack => _backStack.Count > 0;
}
