using ColeHop.Core.Services.Auth;

namespace ColeHop.ViewModel
{
    public sealed class DashboardTeacherViewModel : BaseViewModel
    {
        public DashboardTeacherViewModel(IAuthService auth) : base(auth) { }

        #region Navigation
        public async Task GoToNfcScanAsync() => await Shell.Current.GoToAsync("nfc/scan");

        public async Task GoToPickupListAsync() => await Shell.Current.GoToAsync("pickup/list");

        public async Task LogoutAsync()
        {
            await Auth.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
        #endregion
    }
}
