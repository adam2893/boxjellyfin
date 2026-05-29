using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class ConnectPage : Page
{
    public ConnectViewModel ViewModel { get; }

    public ConnectPage(ConnectViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        Loaded += (s, e) => ServerUrlBox.Focus(FocusState.Programmatic);

        // Toggle UI panels based on connection state
        ViewModel.PropertyChanged += (s, e) =>
        {
            var dq = Windows.System.DispatcherQueue.GetForCurrentThread();
            dq.TryEnqueue(() =>
            {
                bool connected = ViewModel.IsConnected;
                var vis = connected ? Visibility.Visible : Visibility.Collapsed;
                ServerInfoPanel.Visibility = vis;
                ContinueButton.Visibility = vis;
                QuickConnectButton.Visibility = vis;
            });
        };
    }
}
