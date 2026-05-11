using ColeHop.Models;
using ColeHop.Services.Auth;
using ColeHop.Services.Pickup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ColeHop.ViewModels
{
    public sealed partial class DailyPickupListViewModel : BaseViewModel
    {
        private readonly IPickupService _pickupService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private string _todayDate = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DailyPickupItem> _pickupList = new();

        [ObservableProperty]
        private ObservableCollection<DailyPickupItem> _pendingPickups = new();

        [ObservableProperty]
        private ObservableCollection<DailyPickupItem> _completedPickups = new();

        [ObservableProperty]
        private DailyPickupItem? _selectedPickup;

        public DailyPickupListViewModel(IAuthService auth, IPickupService pickupService) : base(auth)
        {
            _pickupService = pickupService;
            UpdateTodayDate();
        }

        private void UpdateTodayDate()
        {
            var culture = new CultureInfo("es-ES");
            var today = DateTime.Today;
            TodayDate = today.ToString("dddd, d 'de' MMMM 'de' yyyy", culture);
            // Capitalizar la primera letra
            TodayDate = char.ToUpper(TodayDate[0]) + TodayDate.Substring(1);
        }

        public async Task InitializeAsync()
        {
            if (CurrentRole != UserRole.Teacher || string.IsNullOrEmpty(Auth.CurrentUserId))
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
                IsRefreshing = true;
                var teacherId = Auth.CurrentUserId!;
                var today = DateOnly.FromDateTime(DateTime.Today);
                var pickups = await _pickupService.GetDailyPickupsAsync(teacherId, today);

                PickupList.Clear();
                PendingPickups.Clear();
                CompletedPickups.Clear();

                foreach (var pickup in pickups)
                {
                    PickupList.Add(pickup);

                    if (pickup.AlreadyPickedUp)
                    {
                        CompletedPickups.Add(pickup);
                    }
                    else
                    {
                        PendingPickups.Add(pickup);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"No se pudieron cargar las recogidas: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
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
