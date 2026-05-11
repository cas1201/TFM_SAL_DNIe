using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Services.TutorManagement;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace ColeHop.ViewModels
{
    public sealed partial class AddAuthorizedPersonViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string dni = string.Empty;

        [ObservableProperty]
        private string relationship = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        public AddAuthorizedPersonViewModel(IAuthService auth, ITutorManagementService tutorManagementService) 
            : base(auth)
        {
            _tutorManagementService = tutorManagementService;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!ValidateInput())
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var tutorId = Auth.CurrentUserId 
                    ?? throw new InvalidOperationException("Usuario no autenticado");

                var personData = new AuthorizedPersonData(
                    Name.Trim(), 
                    LastName.Trim(), 
                    Dni.Trim().ToUpper(), 
                    Relationship.Trim(),
                    []);

                await _tutorManagementService.AddAuthorizedPersonAsync(tutorId, personData);

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error al guardar: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Name))
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

            if (string.IsNullOrWhiteSpace(Relationship))
            {
                HasError = true;
                ErrorMessage = AppResources.RelationshipRequired;
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
