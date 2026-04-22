using ColeHop.Core.Services.Auth;
using ColeHop.Model.Identity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ColeHop.ViewModel
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly IAuthService Auth;

        protected BaseViewModel(IAuthService auth)
        {
            Auth = auth;
        }

        public UserRole? CurrentRole => Auth.CurrentRole;
        public bool IsAuthenticated => Auth.IsAuthenticated;
    }
}