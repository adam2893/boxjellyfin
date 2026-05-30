using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using System.Linq;
using JellyfinClient.Models;
using JellyfinClient.Services;

namespace JellyfinXbox.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b && !b;
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            int count => count > 0 ? Visibility.Visible : Visibility.Collapsed,
            System.Collections.ICollection col => col.Count > 0 ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class TimeSpanFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }
        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class TicksToTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long ticks)
        {
            var ts = TimeSpan.FromTicks(ticks);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                : $"{ts.Minutes}m";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class ItemImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            if (value is not BaseItemDto item)
            {
                App.Log($"[Image] Convert called with non-BaseItemDto: {value?.GetType().Name ?? "null"}");
                return null;
            }
            if (string.IsNullOrEmpty(item.Id))
            {
                App.Log($"[Image] Item has empty Id: '{item.Name}'");
                return null;
            }

            var api = App.GetService<JellyfinApiClient>();
            if (string.IsNullOrEmpty(api.AccessToken))
            {
                App.Log($"[Image] No AccessToken yet for '{item.Name}'");
                return null;
            }
            if (string.IsNullOrEmpty(api.ServerUrl))
            {
                App.Log($"[Image] No ServerUrl yet for '{item.Name}'");
                return null;
            }

            if (item.ImageTags == null || !item.ImageTags.TryGetValue("Primary", out var tag))
            {
                App.Log($"[Image] No Primary tag for '{item.Name}' (ImageTags count={item.ImageTags?.Count ?? 0})");
                return null;
            }

            var maxWidth = parameter is string w && int.TryParse(w, out var p) ? p : 400;
            var imagePath = api.GetImageUrl(item.Id, "Primary", maxWidth, tag: tag);
            var fullUrl = $"{api.ServerUrl}{imagePath}";
            App.Log($"[Image] Loading: {fullUrl}");
            return fullUrl;
        }
        catch (Exception ex)
        {
            App.LogWarn($"[Image] Converter crashed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

