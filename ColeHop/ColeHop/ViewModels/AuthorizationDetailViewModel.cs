using ColeHop.Models;
using ColeHop.Resources.Strings;
using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.Pickup;
using ColeHop.Services.TutorManagement;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    [QueryProperty(nameof(AuthorizationId), "authorizationId")]
    public sealed partial class AuthorizationDetailViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;
        private readonly IPickupService _pickupService;

        [ObservableProperty]
        private string _authorizationId = string.Empty;

        [ObservableProperty]
        private string _personName = string.Empty;

        [ObservableProperty]
        private string _relationship = string.Empty;

        [ObservableProperty]
        private string _childrenNames = string.Empty;

        [ObservableProperty]
        private List<ChildPickupStatus> _childrenList = [];

        [ObservableProperty]
        private DateTime _fromDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _toDate = DateTime.Today;

        private Authorization? _authorization;

        public AuthorizationDetailViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService, IPickupService pickupService)
            : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
            _pickupService = pickupService;
        }

        public async Task InitializeAsync()
        {
            var tutorId = Auth.CurrentUserId;
            if (string.IsNullOrEmpty(tutorId) || string.IsNullOrEmpty(AuthorizationId)) return;

            var authorizations = await _tutorManagementService.GetAuthorizationsAsync(tutorId);
            _authorization = authorizations.FirstOrDefault(a => a.Id == AuthorizationId);

            if (_authorization == null)
            {
                await Alert.ShowAsync(AppResources.Error, AppResources.AuthorizationNotFound, AppResources.OK, AlertIcon.Error);
                await Shell.Current.GoToAsync("..");
                return;
            }

            var persons = await _tutorManagementService.GetAuthorizedPersonsAsync(tutorId);
            var children = await _tutorManagementService.GetChildrenAsync(tutorId);

            var person = persons.FirstOrDefault(p => p.Id == _authorization.AuthorizedPersonId);
            PersonName = person?.FullName ?? AppResources.Unknown;
            Relationship = person?.Relationship ?? "";

            var today = DateOnly.FromDateTime(DateTime.Today);
            var isToday = _authorization.FromDate <= today && _authorization.ToDate >= today;

            // Recogidas del día
            HashSet<string> pickedUpChildIds = [];
            if (isToday)
            {
                var todayPickups = await _pickupService.GetPickupHistoryAsync(tutorId, new PickupHistoryQuery(today, today, null));
                pickedUpChildIds = todayPickups.Select(p => p.ChildId).ToHashSet();
            }

            var childStatuses = _authorization.ChildIds
                .Select(id => children.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null)
                .Select(c => new ChildPickupStatus
                {
                    Name = $"{c!.Name} {c.LastName}",
                    IsPickedUp = pickedUpChildIds.Contains(c.Id)
                })
                .ToList();

            ChildrenNames = string.Join(", ", childStatuses.Select(c => c.Name));
            ChildrenList = childStatuses;

            FromDate = _authorization.FromDate.ToDateTime(TimeOnly.MinValue);
            ToDate = _authorization.ToDate.ToDateTime(TimeOnly.MinValue);
        }

        [RelayCommand]
        private async Task EditAsync()
        {
            if (string.IsNullOrEmpty(AuthorizationId)) return;
            await Shell.Current.GoToAsync($"authorization/create?authorizationId={AuthorizationId}");
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            var confirmed = await Alert.ShowConfirmAsync(
                AppResources.DeleteAuthorization,
                AppResources.ConfirmDeleteAuthorization,
                AppResources.Delete,
                AppResources.Cancel,
                AlertIcon.Warning);

            if (!confirmed) return;

            var tutorId = Auth.CurrentUserId;
            if (string.IsNullOrEmpty(tutorId)) return;

            await _tutorManagementService.DisableAuthorizationAsync(tutorId, AuthorizationId);
            await Shell.Current.GoToAsync("..");
        }
    }
}
