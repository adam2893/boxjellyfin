using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinClient.Models;
using JellyfinXbox.ViewModels;

namespace JellyfinXbox.Views;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }

    public LoginPage(LoginViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // PasswordBox.Password is NOT a dependency property — x:Bind TwoWay doesn't work.
        PasswordBox.PasswordChanged += (s, e) => ViewModel.Password = PasswordBox.Password;

        Loaded += async (s, e) =>
        {
            // Show user list if there are public users
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Focus first field
            if (ViewModel.PublicUsers.Count > 0)
                UserListView.Focus(FocusState.Programmatic);
            else
                UsernameBox.Focus(FocusState.Programmatic);

            // Load users
            await ViewModel.InitializeAsync();
        };
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.PublicUsers) ||
            e.PropertyName == "Item[]")
        {
            var dq = Windows.System.DispatcherQueue.GetForCurrentThread();
            dq.TryEnqueue(() =>
            {
                UserListView.Visibility = ViewModel.PublicUsers.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            });
        }
    }

    private void UserListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is User user)
            ViewModel.SelectUserCommand.Execute(user);
    }

    private void UserListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is User user)
            ViewModel.SelectUserCommand.Execute(user);
    }
}
