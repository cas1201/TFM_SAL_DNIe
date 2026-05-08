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
