using ColeHop.Core.Services.Auth;
using ColeHop.Core.Services.Teacher;
using ColeHop.Model.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModel
{
    public sealed partial class PendingApprovalsViewModel : BaseViewModel
    {
        private readonly ITeacherService _teacherService;

        [ObservableProperty]
        private ObservableCollection<Child> _pendingChildren = new();

        [ObservableProperty]
        private bool _isRefreshing;

        public PendingApprovalsViewModel(IAuthService auth, ITeacherService teacherService)
            : base(auth)
        {
            _teacherService = teacherService;
        }

        public async Task InitializeAsync()
        {
            await LoadPendingAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadPendingAsync();
            IsRefreshing = false;
        }

        private async Task LoadPendingAsync()
        {
            try
            {
                IsBusy = true;
                PendingChildren.Clear();

                var teacherId = Auth.CurrentUserId ?? string.Empty;
                var children = await _teacherService.GetPendingApprovalsAsync(teacherId);

                foreach (var child in children)
                    PendingChildren.Add(child);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ApproveAsync(Child child)
        {
            var teacherId = Auth.CurrentUserId ?? string.Empty;
            await _teacherService.ApproveChildAsync(teacherId, child.Id);
            PendingChildren.Remove(child);
        }

        [RelayCommand]
        private async Task RejectAsync(Child child)
        {
            await Shell.Current.GoToAsync($"rejectreason?childId={child.Id}&childName={Uri.EscapeDataString(child.FullName)}");
        }
    }
}
