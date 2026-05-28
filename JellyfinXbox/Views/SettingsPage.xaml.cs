using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel) { ViewModel = viewModel; InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.LoadFromSettings();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ViewModel.GoBackCommand.Execute(null);
}
