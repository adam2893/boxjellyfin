using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinXbox.ViewModels;
using JellyfinClient.Models;
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
        App.Log("[MediaDetailPage] Constructor — page created");
    }

    public void Initialize(string itemId)
    {
        _pendingItemId = itemId;
        App.Log($"[MediaDetailPage] Initialize called with itemId={itemId}");
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

    private void Play_Click(object sender, RoutedEventArgs e) { ViewModel.PlayItemCommand.Execute(ViewModel.Item); }
    private void Back_Click(object sender, RoutedEventArgs e) { ViewModel.GoBackCommand.Execute(null); }
    private void Episode_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BaseItemDto episode)
            ViewModel.PlayItemCommand.Execute(episode);
    }
}
