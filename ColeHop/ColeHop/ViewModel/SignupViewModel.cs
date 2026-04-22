using ColeHop.Core.Services.Auth;

namespace ColeHop.ViewModel
{
    public sealed class SignupViewModel : BaseViewModel
    {
        public SignupViewModel(IAuthService auth) : base(auth) { }

        #region Navigation
        public async Task GoToLoginAsync() => await Shell.Current.GoToAsync("//login");
        #endregion
    }
}
