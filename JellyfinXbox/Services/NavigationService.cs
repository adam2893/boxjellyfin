using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinXbox.Views;

namespace JellyfinXbox.Services;

/// <summary>
/// Navigation service with persistent page instances for proper back navigation.
/// Frame.Content is set directly (constructor DI pages), and pages are kept alive
/// in a back stack so GoBack restores the exact previous page state.
/// </summary>
public class NavigationService
{
    private Frame? _frame;
    private readonly Stack<Page> _backStack = new();

    public void Initialize(Frame frame) => _frame = frame;

    /// <summary>
    /// Navigate to a new page. Saves the current page to the back stack
    /// so GoBack can restore it. Pages are kept alive — no re-creation.
    /// </summary>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null) return;

        // Don't push if target is a root page (stack gets cleared anyway)
        // or if the current content is already the same type
        var isRoot = pageType == typeof(ConnectPage) || pageType == typeof(HomePage);
        var isSameType = _frame.Content?.GetType() == pageType;

        if (!isRoot && !isSameType && _frame.Content is Page currentPage)
            _backStack.Push(currentPage);

        // Create new page via DI (constructor injection)
        var page = App.Create(pageType) as Page;
        if (page == null) return;

        // Pass parameters
        if (parameter != null)
        {
            if (page is LibraryPage libPage && parameter is ValueTuple<string, string> libTuple)
                libPage.Initialize(libTuple.Item1, libTuple.Item2);
            else if (page is MediaDetailPage detailPage && parameter is string itemId)
                detailPage.Initialize(itemId);
            else if (page is PlayerPage playerPage && parameter is string playerItemId)
                playerPage.Initialize(playerItemId);
        }

        // Clear the old back stack when navigating to a root page
        if (isRoot)
            _backStack.Clear();

        _frame.Content = page;
        _frame.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Restores the previous page from the back stack. Returns false if there's
    /// nothing to go back to.
    /// </summary>
    public bool GoBack()
    {
        if (_frame == null || _backStack.Count == 0)
            return false;

        var previousPage = _backStack.Pop();
        _frame.Content = previousPage;
        _frame.Focus(FocusState.Programmatic);
        return true;
    }

    public bool CanGoBack => _backStack.Count > 0;
}
