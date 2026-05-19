using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    public sealed partial class DashboardTutorViewModel : BaseViewModel
    {
        public DashboardTutorViewModel(IAuthService auth, IAlertService alertService) : base(auth, alertService) { }

        [RelayCommand]
        private async Task GoToChildrenAsync()
        {
            await Shell.Current.GoToAsync("child/manage");
        }

        [RelayCommand]
        private async Task GoToAuthorizedPersonsAsync()
        {
            await Shell.Current.GoToAsync("authorized/manage");
        }

        [RelayCommand]
        private async Task GoToAuthorizationCalendarAsync()
        {
            await Shell.Current.GoToAsync("authorization/manage");
        }

        [RelayCommand]
        private async Task GoToNfcScanAsync()
        {
            await Shell.Current.GoToAsync("nfc/scan");
        }

        [RelayCommand]
        private async Task GoToSettingsAsync()
        {
            await Shell.Current.GoToAsync("settings");
        }


    }
}
