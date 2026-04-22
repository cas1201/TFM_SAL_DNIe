using ColeHop.Core.Services.Auth;

namespace ColeHop.ViewModel
{
    public sealed class LoginViewmodel : BaseViewModel
    {
        public LoginViewmodel(IAuthService auth) : base(auth) { }

        #region Navigation
        public async Task GoToTutorAsync() => await Shell.Current.GoToAsync("dashboard/tutor");

        public async Task GoToTeacherAsync() => await Shell.Current.GoToAsync("dashboard/teacher");

        public async Task GoToSignupAsync() => await Shell.Current.GoToAsync("signup");
        #endregion
    }
}
