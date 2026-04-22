using ColeHop.Core.Services.Auth;

namespace ColeHop.ViewModel
{
    public sealed class DashboardTutorViewModel : BaseViewModel
    {
        public DashboardTutorViewModel(IAuthService auth) : base(auth) { }

        #region Navigation
        public async Task GoToChildrenAsync() => await Shell.Current.GoToAsync("child/manage");

        public async Task GoToAuthorizedAsync() => await Shell.Current.GoToAsync("authorized/manage");

        public async Task GoToAuthorizationAsync() => await Shell.Current.GoToAsync("authorization/manage");

        public async Task LogoutAsync()
        {
            await Auth.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
        #endregion
    }
}
