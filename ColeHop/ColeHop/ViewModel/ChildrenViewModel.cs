using ColeHop.Core.Services.Auth;
using ColeHop.Core.Services.TutorManagement;
using ColeHop.Model.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModel
{
    public sealed partial class ChildrenViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private ObservableCollection<Child> _children = new();

        [ObservableProperty]
        private bool _isRefreshing;

        public ChildrenViewModel(IAuthService auth, ITutorManagementService tutorManagementService) 
            : base(auth)
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
                await Shell.Current.DisplayAlertAsync("Error", $"Error al cargar hijos: {ex.Message}", "OK");
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
        public async Task RefreshAsync()
        {
            await LoadChildrenAsync();
            IsRefreshing = false;
        }
    }
}
