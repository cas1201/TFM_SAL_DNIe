using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    [QueryProperty(nameof(ChildId), "id")]
    public sealed partial class ChildDetailViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private string _childId = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _educationType = string.Empty;

        [ObservableProperty]
        private string _course = string.Empty;

        [ObservableProperty]
        private string _group = string.Empty;

        [ObservableProperty]
        private ApprovalStatus _approvalStatus;

        [ObservableProperty]
        private string? _rejectionReason;

        public bool IsEditable => true;
        public bool ShowRejectionReason => ApprovalStatus == ApprovalStatus.Rejected && !string.IsNullOrEmpty(RejectionReason);

        public ChildDetailViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService) : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
        }

        partial void OnApprovalStatusChanged(ApprovalStatus value)
        {
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(ShowRejectionReason));
        }

        partial void OnChildIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = LoadChildDataAsync();
            }
        }

        private async Task LoadChildDataAsync()
        {
            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var children = await _tutorManagementService.GetChildrenAsync(tutorId);
                var child = children.FirstOrDefault(c => c.Id == ChildId);

                if (child != null)
                {
                    Name = child.Name;
                    LastName = child.LastName;
                    EducationType = child.EducationType;
                    Course = child.Course;
                    Group = child.Group;
                    RejectionReason = child.RejectionReason;
                    ApprovalStatus = child.ApprovalStatus;
                    OnPropertyChanged(nameof(ShowRejectionReason));
                }
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, $"{AppResources.ErrorLoadingData}: {ex.Message}", AppResources.OK, AlertIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveChangesAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Alert.ShowAsync(AppResources.Error, AppResources.NameRequired, AppResources.OK, AlertIcon.Error);
                return;
            }

            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var updatedData = new ChildData(Name, LastName, EducationType, Course, Group);
                await _tutorManagementService.UpdateChildAsync(tutorId, ChildId, updatedData);

                await Alert.ShowAsync(AppResources.Success, AppResources.ChildUpdatedSuccessfully, AppResources.OK, AlertIcon.Success);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, $"{AppResources.ErrorSavingChanges}: {ex.Message}", AppResources.OK, AlertIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            var confirm = await Alert.ShowConfirmAsync(AppResources.ConfirmDeletion, string.Format(AppResources.ConfirmDeleteChild, Name, LastName), AppResources.Delete, AppResources.Cancel, AlertIcon.Warning);

            if (!confirm)
                return;

            try
            {
                IsBusy = true;

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                await _tutorManagementService.RemoveChildAsync(tutorId, ChildId);

                await Alert.ShowAsync(AppResources.Success, AppResources.ChildDeletedSuccessfully, AppResources.OK, AlertIcon.Success);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, $"{AppResources.ErrorDeleting}: {ex.Message}", AppResources.OK, AlertIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
