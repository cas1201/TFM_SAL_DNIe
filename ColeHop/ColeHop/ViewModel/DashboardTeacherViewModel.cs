using ColeHop.Core.Services.Auth;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModel
{
    public sealed partial class DashboardTeacherViewModel : BaseViewModel
    {
        public DashboardTeacherViewModel(IAuthService auth) : base(auth) { }

        [RelayCommand]
        private async Task GoToDailyPickupListAsync()
        {
            await Shell.Current.GoToAsync("pickup/list");
        }

        [RelayCommand]
        private async Task GoToNfcScanAsync()
        {
            await Shell.Current.GoToAsync("nfc/scan");
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            // El evento AuthenticationStateChanged en AppShell se encargará de mostrar LoginPage
            await Auth.LogoutAsync();
        }
    }
}
