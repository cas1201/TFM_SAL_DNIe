using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
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

        public AuthorizedPersonDetailViewModel(IAuthService auth, ITutorManagementService tutorManagementService) 
            : base(auth)
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
                    // Parseamos el FullName para extraer nombre y apellidos
                    var fullNameParts = person.FullName.Split(' ', 2);
                    Name = fullNameParts.Length > 0 ? fullNameParts[0] : "";
                    LastName = fullNameParts.Length > 1 ? fullNameParts[1] : "";

                    // Por ahora usamos valores por defecto ya que el modelo AuthorizedPerson solo tiene FullName
                    Dni = "N/A";
                    Relationship = "N/A";
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Error al cargar datos: {ex.Message}", "OK");
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
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.NameRequired, AppResources.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.LastNameRequired, AppResources.OK);
                return;
            }

            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var updatedData = new AuthorizedPersonData(Name, LastName, Dni, Relationship, Array.Empty<byte>());
                await _tutorManagementService.UpdateAuthorizedPersonAsync(tutorId, PersonId, updatedData);

                await Shell.Current.DisplayAlertAsync(AppResources.Success, AppResources.PersonUpdatedSuccessfully, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, $"{AppResources.ErrorSavingChanges}: {ex.Message}", AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
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

                await Shell.Current.DisplayAlertAsync(AppResources.Success, AppResources.PersonDeletedSuccessfully, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, $"{AppResources.ErrorDeleting}: {ex.Message}", AppResources.OK);
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
