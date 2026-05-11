using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Services.TutorManagement;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    public sealed partial class AddChildViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string educationType = "Primaria";

        [ObservableProperty]
        private string course = string.Empty;

        [ObservableProperty]
        private string group = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        public AddChildViewModel(IAuthService auth, ITutorManagementService tutorManagementService) 
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

                var childData = new ChildData(
                    Name.Trim(), 
                    LastName.Trim(), 
                    EducationType.Trim(), 
                    Course.Trim(), 
                    Group.Trim());
                await _tutorManagementService.AddChildAsync(tutorId, childData);

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"{AppResources.ErrorSaving}: {ex.Message}";
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
                ErrorMessage = "El nombre es obligatorio";
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                HasError = true;
                ErrorMessage = "Los apellidos son obligatorios";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EducationType))
            {
                HasError = true;
                ErrorMessage = "El tipo de enseñanza es obligatorio";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Course))
            {
                HasError = true;
                ErrorMessage = "El curso es obligatorio";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Group))
            {
                HasError = true;
                ErrorMessage = "El grupo es obligatorio";
                return false;
            }

            return true;
        }
    }
}
