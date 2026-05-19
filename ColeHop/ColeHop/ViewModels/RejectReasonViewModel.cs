using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.Teacher;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    [QueryProperty(nameof(ChildId), "childId")]
    [QueryProperty(nameof(ChildName), "childName")]
    public sealed partial class RejectReasonViewModel : BaseViewModel
    {
        private readonly ITeacherService _teacherService;

        [ObservableProperty]
        private string _childId = string.Empty;

        [ObservableProperty]
        private string _childName = string.Empty;

        [ObservableProperty]
        private string _reason = string.Empty;

        public RejectReasonViewModel(IAuthService auth, IAlertService alertService, ITeacherService teacherService) : base(auth, alertService)
        {
            _teacherService = teacherService;
        }

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            if (string.IsNullOrWhiteSpace(Reason))
            {
                await Alert.ShowAsync(
                    AppResources.Error,
                    AppResources.EnterRejectionReason,
                    AppResources.OK);
                return;
            }

            var teacherId = Auth.CurrentUserId ?? string.Empty;
            await _teacherService.RejectChildAsync(teacherId, ChildId, Reason);
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
