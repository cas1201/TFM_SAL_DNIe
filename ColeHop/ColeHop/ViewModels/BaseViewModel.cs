using ColeHop.Services.Auth;
using ColeHop.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ColeHop.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isBusy;

        protected readonly IAuthService Auth;

        protected BaseViewModel(IAuthService auth)
        {
            Auth = auth;
        }

        public UserRole? CurrentRole => Auth.CurrentRole;
        public bool IsAuthenticated => Auth.IsAuthenticated;
    }
}