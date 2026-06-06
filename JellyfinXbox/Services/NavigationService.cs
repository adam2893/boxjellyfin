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
    private readonly Dictionary<Type, Page> _cachedRoots = new();

    public void Initialize(Frame frame) => _frame = frame;

    /// <summary>
    /// Navigate to a new page. Saves the current page to the back stack
    /// so GoBack can restore it. Pages are kept alive — no re-creation.
    /// </summary>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null) { App.LogWarn("[Nav] Frame is null — cannot navigate"); return; }

        NotifyNavigatedAway();

        var currentType = _frame.Content?.GetType();
        App.Log($"[Nav] NavigateTo: {currentType?.Name ?? "null"} → {pageType.Name}, param={parameter?.GetType().Name ?? "null"}");

        // Don't navigate if we're already on the target page type
        if (currentType == pageType)
        {
            App.Log($"[Nav] Already on {pageType.Name} — skipping");
            return;
        }

        // Don't push root pages to back stack
        var isRoot = pageType == typeof(ConnectPage) || pageType == typeof(HomePage);

        if (!isRoot && _frame.Content is Page currentPage)
            _backStack.Push(currentPage);

        // Reuse cached root pages to avoid full reload on every navigation
        Page? page = null;
        if (isRoot && _cachedRoots.TryGetValue(pageType, out var cached))
        {
            page = cached;
            App.Log($"[Nav] Reusing cached {pageType.Name}");
        }
        else
        {
            try
            {
                page = App.Create(pageType) as Page;
                if (isRoot && page != null)
                    _cachedRoots[pageType] = page;
            }
            catch (Exception ex)
            {
                App.LogWarn($"[Nav] Failed to create {pageType.Name}: {ex.GetType().Name}: {ex.Message}");
                if (!isRoot && _backStack.Count > 0) _backStack.Pop();
                return;
            }
        }

        if (page == null)
        {
            App.LogWarn($"[Nav] App.Create returned null for {pageType.Name}");
            if (!isRoot && _backStack.Count > 0) _backStack.Pop();
            return;
        }

        App.Log($"[Nav] Created {pageType.Name} successfully");

        // Pass parameters
        if (parameter != null)
        {
            if (page is LibraryPage libPage && parameter is ValueTuple<string, string> libTuple)
            {
                libPage.Initialize(libTuple.Item1, libTuple.Item2);
                App.Log($"[Nav] LibraryPage initialized: {libTuple.Item1}, {libTuple.Item2}");
            }
            else if (page is MediaDetailPage detailPage && parameter is string itemId)
            {
                detailPage.Initialize(itemId);
                App.Log($"[Nav] MediaDetailPage initialized with itemId={itemId}");
            }
            else if (page is PlayerPage playerPage && parameter is string playerItemId)
            {
                playerPage.Initialize(playerItemId);
                App.Log($"[Nav] PlayerPage initialized with itemId={playerItemId}");
            }
            else
            {
                App.LogWarn($"[Nav] Unhandled parameter type: {parameter.GetType().Name} for page {pageType.Name}");
            }
        }

        // Clear the old back stack when navigating to a root page
        if (isRoot)
            _backStack.Clear();

        _frame.Content = page;
        _frame.Focus(FocusState.Programmatic);
        App.Log($"[Nav] ✓ Navigation complete: {pageType.Name}");
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

    public event Action<Type>? NavigatedAway;

    /// <summary>
    /// Notify the current page that it is being navigated away from.
    /// </summary>
    private void NotifyNavigatedAway()
    {
        if (_frame?.Content is Page current)
            NavigatedAway?.Invoke(current.GetType());
    }
}
