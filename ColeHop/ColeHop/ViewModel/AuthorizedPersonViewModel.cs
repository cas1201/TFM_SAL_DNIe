using ColeHop.Core.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModel
{
    public sealed partial class AuthorizedPersonViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<AuthorizedPersonItem> _authorizedPersons = new();

        public AuthorizedPersonViewModel(IAuthService auth) : base(auth) { }

        public void Initialize()
        {
            // Datos simulados para navegación básica
            AuthorizedPersons.Clear();
            AuthorizedPersons.Add(new AuthorizedPersonItem("María López", "Madre"));
            AuthorizedPersons.Add(new AuthorizedPersonItem("Carlos Pérez", "Padre"));
            AuthorizedPersons.Add(new AuthorizedPersonItem("Laura Sánchez", "Abuela"));
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    // Modelo simple para la lista
    public sealed record AuthorizedPersonItem(string FullName, string Relationship);
}
