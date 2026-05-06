using ColeHop.Core.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModel
{
    public sealed partial class SignupViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        public SignupViewModel(IAuthService auth) : base(auth) { }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            // Navegación básica sin lógica de registro real
            await Shell.Current.DisplayAlertAsync("Registro", "Funcionalidad de registro pendiente", "OK");
        }

        [RelayCommand]
        private async Task GoToLoginAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
