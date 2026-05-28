using System;
using Windows.UI.Xaml.Controls;

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

        _frame.Content = page;
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;
}
