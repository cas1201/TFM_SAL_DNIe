using ColeHop.Core.Services.Auth;
using ColeHop.Core.Services.Pickup;
using ColeHop.Core.Services.Pickup.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModel
{
    public sealed partial class DailyPickupListViewModel : BaseViewModel
    {
        private readonly IPickupService _pickupService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<DailyPickupItem> _pickupList = new();

        [ObservableProperty]
        private DailyPickupItem? _selectedPickup;

        public DailyPickupListViewModel(IAuthService auth, IPickupService pickupService) : base(auth)
        {
            _pickupService = pickupService;
        }

        public async Task InitializeAsync()
        {
            if (CurrentRole != Model.Identity.UserRole.Teacher || string.IsNullOrEmpty(Auth.CurrentUserId))
            {
                await Shell.Current.DisplayAlertAsync("Error", "Acceso no autorizado", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            await LoadPickupsAsync();
        }

        [RelayCommand]
        private async Task LoadPickupsAsync()
        {
            try
            {
                IsLoading = true;
                var teacherId = Auth.CurrentUserId!;
                var today = DateOnly.FromDateTime(DateTime.Today);
                var pickups = await _pickupService.GetDailyPickupsAsync(teacherId, today);

                PickupList.Clear();
                foreach (var pickup in pickups)
                {
                    PickupList.Add(pickup);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"No se pudieron cargar las recogidas: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SelectPickupAsync(DailyPickupItem pickup)
        {
            if (pickup.AlreadyPickedUp)
            {
                await Shell.Current.DisplayAlertAsync("Información", "Este niño ya ha sido recogido hoy", "OK");
                return;
            }

            SelectedPickup = pickup;
            await Shell.Current.GoToAsync($"nfc/scan?childId={pickup.ChildId}&childName={pickup.ChildName}");
        }
    }
}
