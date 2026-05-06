using ColeHop.Core.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModel
{
    public sealed partial class AuthorizationViewModel : BaseViewModel
    {
        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _minimumDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<AuthChildItem> _children = new();

        [ObservableProperty]
        private ObservableCollection<object> _selectedChildren = new();

        [ObservableProperty]
        private ObservableCollection<AuthPersonItem> _authorizedPeople = new();

        [ObservableProperty]
        private AuthPersonItem? _selectedAuthorizedPerson;

        public AuthorizationViewModel(IAuthService auth) : base(auth) { }

        public void Initialize()
        {
            // Datos simulados
            Children.Clear();
            Children.Add(new AuthChildItem("Juan Pérez", "child-1"));
            Children.Add(new AuthChildItem("Ana García", "child-2"));

            AuthorizedPeople.Clear();
            AuthorizedPeople.Add(new AuthPersonItem("María López", "authorized-1"));
            AuthorizedPeople.Add(new AuthPersonItem("Carlos Pérez", "authorized-2"));
        }

        [RelayCommand]
        private async Task CreateAuthorizationAsync()
        {
            await Shell.Current.DisplayAlertAsync("Autorización", "Autorización creada (simulada)", "OK");
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public sealed record AuthChildItem(string Name, string Id);
    public sealed record AuthPersonItem(string FullName, string Id);
}
