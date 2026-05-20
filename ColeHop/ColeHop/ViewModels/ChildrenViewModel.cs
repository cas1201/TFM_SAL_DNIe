using ColeHop.Resources.Strings;
using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModels
{
    public sealed partial class ChildrenViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private ObservableCollection<Child> _children = new();

        [ObservableProperty]
        private bool _isRefreshing;

        public ChildrenViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService) 
            : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
        }

        public async Task InitializeAsync()
        {
            await LoadChildrenAsync();
        }

        private async Task LoadChildrenAsync()
        {
            try
            {
                IsBusy = true;
                Children.Clear();

                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var children = await _tutorManagementService.GetChildrenAsync(tutorId);

                foreach (var child in children)
                {
                    Children.Add(child);
                }
            }
            catch (Exception ex)
            {
                await Alert.ShowAsync(AppResources.Error, string.Format(AppResources.ErrorLoadingChildren, ex.Message), AppResources.OK, AlertIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddChildAsync()
        {
            await Shell.Current.GoToAsync("addchild");
        }

        [RelayCommand]
        private async Task SelectChildAsync(Child child)
        {
            if (child == null)
                return;

            await Shell.Current.GoToAsync($"childdetail?id={child.Id}");
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadChildrenAsync();
            IsRefreshing = false;
        }
    }
}
