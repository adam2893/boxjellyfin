using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinXbox.ViewModels;
using JellyfinClient.Services;

namespace JellyfinXbox.Views;

public sealed partial class MediaDetailPage : Page
{
    public MediaDetailViewModel ViewModel { get; }

    public MediaDetailPage(MediaDetailViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string itemId)
            _ = LoadSafeAsync(itemId);
    }

    private async Task LoadSafeAsync(string id)
    {
        try
        {
            await ViewModel.LoadAsync(id);
            LoadImages();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Detail load failed: {ex.Message}"); }
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
