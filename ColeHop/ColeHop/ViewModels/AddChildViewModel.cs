using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
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
        private string _name = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _educationType = "Primaria";

        [ObservableProperty]
        private string _course = string.Empty;

        [ObservableProperty]
        private string _group = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public AddChildViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService) 
            : base(auth, alertService)
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
                ErrorMessage = AppResources.NameRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                HasError = true;
                ErrorMessage = AppResources.LastNameRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(EducationType))
            {
                HasError = true;
                ErrorMessage = AppResources.EducationTypeRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(Course))
            {
                HasError = true;
                ErrorMessage = AppResources.CourseRequired;
                return false;
            }

            if (string.IsNullOrWhiteSpace(Group))
            {
                HasError = true;
                ErrorMessage = AppResources.GroupRequired;
                return false;
            }

            return true;
        }
    }
}
