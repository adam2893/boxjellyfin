using System.Windows.Input;
using JellyfinXbox.Services;

namespace JellyfinXbox.ViewModels;

public class ShellViewModel : ObservableObject
{
    private readonly Services.NavigationService _nav;

    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateSearchCommand { get; }

    public ShellViewModel(Services.NavigationService nav)
    {
        _nav = nav;
        NavigateHomeCommand = new RelayCommand(NavigateHome);
        NavigateSearchCommand = new RelayCommand(NavigateSearch);
    }

    private void NavigateHome() => _nav.NavigateTo(typeof(Views.HomePage));
    private void NavigateSearch() => _nav.NavigateTo(typeof(Views.SearchPage));
}
