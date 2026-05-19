using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    [QueryProperty(nameof(PersonId), "id")]
    public sealed partial class AuthorizedPersonDetailViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private string _personId = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _dni = string.Empty;

        [ObservableProperty]
        private string _relationship = string.Empty;

        public AuthorizedPersonDetailViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService) 
            : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
        }

        partial void OnPersonIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = LoadPersonDataAsync();
            }
        }

        private async Task LoadPersonDataAsync()
        {
            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var persons = await _tutorManagementService.GetAuthorizedPersonsAsync(tutorId);
                var person = persons.FirstOrDefault(p => p.Id == PersonId);

                if (person != null)
                {
                    Name = person.Name;
                    LastName = person.LastName;
                    Dni = person.Dni;
                    Relationship = person.Relationship;
                }
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, string.Format(AppResources.ErrorLoadingData, ex.Message));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveChangesAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Alert.ShowAsync(AppResources.Error, AppResources.NameRequired, AppResources.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                await Alert.ShowAsync(AppResources.Error, AppResources.LastNameRequired, AppResources.OK);
                return;
            }

            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var updatedData = new AuthorizedPersonData(Name, LastName, Dni, Relationship);
                await _tutorManagementService.UpdateAuthorizedPersonAsync(tutorId, PersonId, updatedData);

                await Alert.ShowAsync(AppResources.Success, AppResources.PersonUpdatedSuccessfully, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, $"{AppResources.ErrorSavingChanges}: {ex.Message}", AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            var confirm = await Alert.ShowConfirmAsync(
                AppResources.ConfirmDeletion,
                $"{AppResources.ConfirmDeletePerson.Replace("{0}", Name).Replace("{1}", LastName)}",
                AppResources.Delete,
                AppResources.Cancel);

            if (!confirm)
                return;

            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                await _tutorManagementService.RemoveAuthorizedPersonAsync(tutorId, PersonId);

                await Alert.ShowAsync(AppResources.Success, AppResources.PersonDeletedSuccessfully, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, $"{AppResources.ErrorDeleting}: {ex.Message}", AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
