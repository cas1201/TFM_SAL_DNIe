using ColeHop.Resources.Strings;
using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModels
{
    public sealed partial class AuthorizedPersonViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private ObservableCollection<AuthorizedPerson> _authorizedPersons = new();

        [ObservableProperty]
        private bool _isRefreshing;

        public AuthorizedPersonViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService) 
            : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
        }

        public async Task InitializeAsync()
        {
            await LoadAuthorizedPersonsAsync();
        }

        private async Task LoadAuthorizedPersonsAsync()
        {
            try
            {
                IsBusy = true;
                AuthorizedPersons.Clear();

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var persons = await _tutorManagementService.GetAuthorizedPersonsAsync(tutorId);

                foreach (var person in persons)
                {
                    AuthorizedPersons.Add(person);
                }
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, string.Format(AppResources.ErrorLoadingAuthorizedPersons, ex.Message));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddAuthorizedPersonAsync()
        {
            await Shell.Current.GoToAsync("addauthorizedperson");
        }

        [RelayCommand]
        private async Task SelectAuthorizedPersonAsync(AuthorizedPerson person)
        {
            if (person == null)
                return;

            await Shell.Current.GoToAsync($"authorizedpersondetail?id={person.Id}");
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadAuthorizedPersonsAsync();
            IsRefreshing = false;
        }
    }
}
