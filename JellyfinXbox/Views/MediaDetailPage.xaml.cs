using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinXbox.ViewModels;
using JellyfinClient.Services;

namespace JellyfinXbox.Views;

public sealed partial class MediaDetailPage : Page
{
    public MediaDetailViewModel ViewModel { get; }

    private string? _pendingItemId;

    public MediaDetailPage(MediaDetailViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void Initialize(string itemId)
    {
        _pendingItemId = itemId;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_pendingItemId != null)
        {
            try
            {
                await ViewModel.LoadAsync(_pendingItemId);
                LoadImages();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Detail load failed: {ex.Message}"); }
        }
    }

    private void LoadImages()
    {
        var api = App.GetService<JellyfinApiClient>();

        if (!string.IsNullOrEmpty(ViewModel.BackdropUrl))
            BackdropImage.Source = new BitmapImage(new Uri($"{api.ServerUrl}{ViewModel.BackdropUrl}"));

        if (!string.IsNullOrEmpty(ViewModel.PosterUrl))
            PosterImage.Source = new BitmapImage(new Uri($"{api.ServerUrl}{ViewModel.PosterUrl}"));
    }

    private void Play_Click(object sender, RoutedEventArgs e) { ViewModel.PlayItemCommand.Execute(null); }
    private void Back_Click(object sender, RoutedEventArgs e) { ViewModel.GoBackCommand.Execute(null); }
}
