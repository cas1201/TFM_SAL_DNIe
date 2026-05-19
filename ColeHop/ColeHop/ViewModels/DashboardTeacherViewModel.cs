using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    public sealed partial class DashboardTeacherViewModel : BaseViewModel
    {
        public DashboardTeacherViewModel(IAuthService auth, IAlertService alertService) : base(auth, alertService) { }

        [RelayCommand]
        private async Task GoToDailyPickupListAsync()
        {
            await Shell.Current.GoToAsync("pickup/list");
        }

        [RelayCommand]
        private async Task GoToPendingApprovalsAsync()
        {
            await Shell.Current.GoToAsync("pendingapprovals");
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