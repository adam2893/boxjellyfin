using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using JellyfinClient.Models;
using System.Linq;
using JellyfinXbox.Services;
using JellyfinXbox.ViewModels;
using JellyfinXbox.Views;

namespace JellyfinXbox.Views;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }
    private readonly NavigationService _nav;

    public LoginPage(NavigationService nav, LoginViewModel viewModel)
    {
        ViewModel = viewModel;
        _nav = nav;
        InitializeComponent();
        Loaded += (s, e) => ServerUrlBox.Focus(FocusState.Programmatic);
    }

    private void ServerUrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            ViewModel.ConnectCommand.Execute(null);
    }

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            ViewModel.LoginCommand.Execute(null);
    }

    private void UserListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is User user)
            ViewModel.SelectUserCommand.Execute(user);
    }

    private void QuickConnect_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo(typeof(QuickConnectPage));
    }

    private void UserListView_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is JellyfinClient.Models.User user)
            ViewModel.SelectUserCommand.Execute(user);
    }
}