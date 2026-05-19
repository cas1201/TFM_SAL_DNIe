using ColeHop.Services.Alert;
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
        protected readonly IAlertService Alert;

        protected BaseViewModel(IAuthService auth, IAlertService alertService)
        {
            Auth = auth;
            Alert = alertService;
        }

        public UserRole? CurrentRole => Auth.CurrentRole;
        public bool IsAuthenticated => Auth.IsAuthenticated;
    }
}