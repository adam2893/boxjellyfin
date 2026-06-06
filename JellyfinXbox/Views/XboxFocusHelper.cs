using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace JellyfinXbox.Views;

public static class XboxFocusHelper
{
    public static void WireButton(Button button, Action onActivate)
    {
        button.IsTabStop = true;
        button.UseSystemFocusVisuals = false;
        button.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.GamepadA || e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                onActivate();
                e.Handled = true;
            }
        };
    }

    public static void WireGridViewActivate(GridView grid, Action<object> onItem)
    {
        grid.IsFocusEngagementEnabled = true;
        grid.KeyDown += (_, e) =>
        {
            if (e.Key != VirtualKey.GamepadA && e.Key != VirtualKey.Enter) return;
            if (TryGetFocusedItem(grid, out var item) && item != null)
            {
                onItem(item);
                e.Handled = true;
            }
        };
    }

    public static bool TryGetFocusedItem(GridView grid, out object? item)
    {
        item = null;
        var focused = FocusManager.GetFocusedElement();
        if (focused is GridViewItem gvi)
        {
            item = gvi.Content;
            return item != null;
        }
        if (focused is FrameworkElement fe)
        {
            var parent = fe;
            while (parent != null)
            {
                if (parent is GridViewItem gi)
                {
                    item = gi.Content;
                    return item != null;
                }
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }
        }
        return false;
    }

    public static void WireListViewActivate(ListView list, Action<object> onItem)
    {
        list.IsFocusEngagementEnabled = true;
        list.KeyDown += (_, e) =>
        {
            if (e.Key != VirtualKey.GamepadA && e.Key != VirtualKey.Enter) return;
            if (TryGetFocusedListItem(list, out var item) && item != null)
            {
                onItem(item);
                e.Handled = true;
            }
        };
    }

    public static bool TryGetFocusedListItem(ListView list, out object? item)
    {
        item = null;
        var focused = FocusManager.GetFocusedElement();
        if (focused is ListViewItem lvi)
        {
            item = lvi.Content;
            return item != null;
        }
        if (focused is FrameworkElement fe)
        {
            var parent = fe;
            while (parent != null)
            {
                if (parent is ListViewItem li)
                {
                    item = li.Content;
                    return item != null;
                }
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }
        }
        return false;
    }
}
