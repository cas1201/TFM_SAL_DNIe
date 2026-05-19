using ColeHop.Models;
using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.Pickup;
using ColeHop.Services.TutorManagement;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModels
{
    public sealed partial class AuthorizationListViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;
        private readonly IPickupService _pickupService;

        [ObservableProperty]
        private ObservableCollection<AuthorizationDisplayItem> _activeAuthorizations = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isEmpty;

        public AuthorizationListViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService, IPickupService pickupService)
            : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
            _pickupService = pickupService;
        }

        public async Task InitializeAsync()
        {
            await LoadAuthorizationsAsync();
        }

        [RelayCommand]
        private async Task LoadAuthorizationsAsync()
        {
            try
            {
                IsLoading = true;
                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId)) return;

                var authorizations = await _tutorManagementService.GetAuthorizationsAsync(tutorId);
                var children = await _tutorManagementService.GetChildrenAsync(tutorId);
                var persons = await _tutorManagementService.GetAuthorizedPersonsAsync(tutorId);

                var today = DateOnly.FromDateTime(DateTime.Today);

                // Obtener recogidas del día para marcar autorizaciones completadas
                var todayPickups = await _pickupService.GetPickupHistoryAsync(tutorId, new PickupHistoryQuery(today, today, null));
                var pickedUpChildIds = todayPickups.Select(p => p.ChildId).ToHashSet();

                var active = authorizations
                    .Where(a => a.IsActive && a.ToDate >= today)
                    .OrderBy(a => a.FromDate)
                    .ToList();

                ActiveAuthorizations.Clear();

                foreach (var auth in active)
                {
                    var person = persons.FirstOrDefault(p => p.Id == auth.AuthorizedPersonId);
                    var childNames = auth.ChildIds
                        .Select(id => children.FirstOrDefault(c => c.Id == id))
                        .Where(c => c != null)
                        .Select(c => $"{c!.Name} {c.LastName}")
                        .ToList();

                    var isToday = auth.FromDate <= today && auth.ToDate >= today;
                    var pickedCount = isToday ? auth.ChildIds.Count(id => pickedUpChildIds.Contains(id)) : 0;
                    var isCompleted = isToday && pickedCount == auth.ChildIds.Count;

                    ActiveAuthorizations.Add(new AuthorizationDisplayItem
                    {
                        Id = auth.Id,
                        PersonName = person?.FullName ?? "Desconocido",
                        Relationship = person?.Relationship ?? "",
                        ChildrenNames = string.Join(", ", childNames),
                        ChildrenList = childNames,
                        FromDate = auth.FromDate,
                        ToDate = auth.ToDate,
                        DateRange = $"{auth.FromDate:dd/MM/yyyy} - {auth.ToDate:dd/MM/yyyy}",
                        IsToday = isToday,
                        IsCompleted = isCompleted,
                        PickedUpCount = pickedCount,
                        TotalChildren = auth.ChildIds.Count
                    });
                }

                IsEmpty = ActiveAuthorizations.Count == 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreateAuthorizationAsync()
        {
            await Shell.Current.GoToAsync("authorization/create");
        }

        [RelayCommand]
        private async Task OpenAuthorizationAsync(AuthorizationDisplayItem item)
        {
            await Shell.Current.GoToAsync($"authorization/detail?authorizationId={item.Id}");
        }
    }

    public sealed class AuthorizationDisplayItem
    {
        public string Id { get; init; } = default!;
        public string PersonName { get; init; } = default!;
        public string Relationship { get; init; } = default!;
        public string ChildrenNames { get; init; } = default!;
        public List<string> ChildrenList { get; init; } = [];
        public DateOnly FromDate { get; init; }
        public DateOnly ToDate { get; init; }
        public string DateRange { get; init; } = default!;
        public bool IsToday { get; init; }
        public bool IsCompleted { get; init; }
        public int PickedUpCount { get; init; }
        public int TotalChildren { get; init; }
        public bool HasPartialProgress => IsToday && PickedUpCount > 0 && !IsCompleted;
        public string ProgressText => $"{PickedUpCount}/{TotalChildren}";
    }
}
