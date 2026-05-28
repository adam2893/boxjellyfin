using Windows.UI.Xaml;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class QuickConnectPage : Page
{
    public QuickConnectViewModel ViewModel { get; }

    public QuickConnectPage(QuickConnectViewModel viewModel) { ViewModel = viewModel; InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            Windows.System.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                bool isActive = ViewModel.IsActive;
                bool isSuccess = ViewModel.IsSuccess;
                bool isError = ViewModel.IsError;

                StartPanel.Visibility = (!isActive && !isSuccess && !isError) ? Visibility.Visible : Visibility.Collapsed;
                ConnectingPanel.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
                SuccessPanel.Visibility = isSuccess ? Visibility.Visible : Visibility.Collapsed;
                ErrorPanel.Visibility = isError ? Visibility.Visible : Visibility.Collapsed;
            });
        };
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ViewModel.GoBackCommand.Execute(null);
}
