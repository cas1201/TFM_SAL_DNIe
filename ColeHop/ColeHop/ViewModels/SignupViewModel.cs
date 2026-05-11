using ColeHop.Services.Auth;
using ColeHop.Services.Auth;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace ColeHop.ViewModels
{
    public sealed partial class SignupViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _dni = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _school = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public SignupViewModel(IAuthService auth) : base(auth) { }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (!ValidateInput())
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var registrationData = new TutorRegistrationData(
                    FirstName.Trim(),
                    LastName.Trim(),
                    Dni.Trim().ToUpper(),
                    Email.Trim(),
                    Password,
                    School.Trim()
                );

                await Auth.RegisterTutorAsync(registrationData);
                await Shell.Current.DisplayAlertAsync(AppResources.Registration, AppResources.RegistrationCompletedSuccessfully, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"{AppResources.ErrorRegistering}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToLoginAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                HasError = true;
                ErrorMessage = AppResources.NameRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                HasError = true;
                ErrorMessage = AppResources.LastNameRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(Dni))
            {
                HasError = true;
                ErrorMessage = AppResources.DniRequired;
                return false;
            }

            if (!IsValidDni(Dni))
            {
                HasError = true;
                ErrorMessage = AppResources.DniInvalidFormat;
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                HasError = true;
                ErrorMessage = AppResources.EmailRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                HasError = true;
                ErrorMessage = AppResources.PasswordRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(School))
            {
                HasError = true;
                ErrorMessage = AppResources.SchoolRequired;
                return false;
            }

            return true;
        }

        private bool IsValidDni(string dni)
        {
            var regex = new Regex(@"^\d{8}[A-Z]$");
            return regex.IsMatch(dni.ToUpper().Trim());
        }
    }
}
