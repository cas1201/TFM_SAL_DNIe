using ColeHop.Services.Auth;
using ColeHop.Services.Nfc;
using ColeHop.Services.Nfc;
using ColeHop.Services.Pickup;
using ColeHop.Services.Pickup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    [QueryProperty(nameof(ChildId), "childId")]
    [QueryProperty(nameof(ChildName), "childName")]
    public sealed partial class NfcScanViewModel : BaseViewModel
    {
        private readonly INfcService _nfcService;
        private readonly IPickupService _pickupService;
        private CancellationTokenSource? _scanCts;
        private PickupContext? _currentContext;

        [ObservableProperty]
        private string _childId = string.Empty;

        [ObservableProperty]
        private string _childName = string.Empty;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _scanStatus = "Esperando DNI electrónico...";

        [ObservableProperty]
        private string _can = string.Empty;

        [ObservableProperty]
        private bool _showCanInput = true;

        public NfcScanViewModel(IAuthService auth, INfcService nfcService, IPickupService pickupService) : base(auth)
        {
            _nfcService = nfcService;
            _pickupService = pickupService;
            _nfcService.IdentityVerified += OnIdentityVerified;
        }

        public async Task InitializeAsync()
        {
            if (!_nfcService.IsSupported)
            {
                ScanStatus = "Este dispositivo no soporta NFC";
                await Shell.Current.DisplayAlertAsync("Error", "NFC no soportado", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (!_nfcService.IsEnabled)
            {
                ScanStatus = "Por favor, active NFC en los ajustes";
                await Shell.Current.DisplayAlertAsync("Aviso", "NFC desactivado. Actívelo para continuar.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (string.IsNullOrEmpty(ChildId))
            {
                await Shell.Current.DisplayAlertAsync("Error", "No se ha especificado el niño a recoger", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                var teacherId = Auth.CurrentUserId!;
                var today = DateOnly.FromDateTime(DateTime.Today);
                _currentContext = await _pickupService.StartPickupAsync(teacherId, ChildId, today);
                ScanStatus = $"Listo para escanear DNI para recoger a {ChildName}";
            }
            catch (Exception ex)
            {
                ScanStatus = "Error al iniciar recogida";
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
                await Shell.Current.GoToAsync("..");
            }
        }

        [RelayCommand]
        private async Task StartScanAsync()
        {
            if (string.IsNullOrWhiteSpace(Can) || Can.Length != 6)
            {
                await Shell.Current.DisplayAlertAsync("Error", "El CAN debe tener 6 dígitos", "OK");
                return;
            }

            if (_currentContext == null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Contexto de recogida no válido", "OK");
                return;
            }

            try
            {
                IsScanning = true;
                ShowCanInput = false;
                ScanStatus = "Acerque el DNI electrónico al lector... (10 segundos)";

                // Crear CancellationTokenSource con timeout de 10 segundos
                _scanCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _nfcService.BeginDnieReadingAsync(Can, _scanCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Verificar si fue timeout o cancelación manual
                if (_scanCts?.IsCancellationRequested == true && _scanCts.Token.IsCancellationRequested)
                {
                    // Fue timeout
                    IsScanning = false;
                    ShowCanInput = true;
                    ScanStatus = "Tiempo de espera agotado. Intente de nuevo.";
                    await EnsureNfcStoppedAsync();
                    await Shell.Current.DisplayAlertAsync(
                        "Tiempo agotado", 
                        "No se detectó el DNI en 10 segundos. Por favor, intente de nuevo.", 
                        "OK");
                }
                else
                {
                    // Fue cancelación manual
                    ScanStatus = "Escaneo cancelado";
                    await EnsureNfcStoppedAsync();
                }
            }
            catch (Exception ex)
            {
                IsScanning = false;
                ShowCanInput = true;
                ScanStatus = $"Error: {ex.Message}";
                await EnsureNfcStoppedAsync();
                await Shell.Current.DisplayAlertAsync("Error de verificación", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task CancelScanAsync()
        {
            _scanCts?.Cancel();
            IsScanning = false;
            ShowCanInput = true;
            ScanStatus = "Escaneo cancelado";
            await EnsureNfcStoppedAsync();
        }

        private async Task EnsureNfcStoppedAsync()
        {
            try
            {
                await _nfcService.StopAsync();
            }
            catch
            {
                // Ignorar errores al detener NFC
            }
        }

        public async Task CleanupAsync()
        {
            // Cancelar cualquier escaneo en curso
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;

            // Desactivar foreground dispatch NFC
            try
            {
                await _nfcService.StopAsync();
            }
            catch
            {
                // Ignorar errores al detener NFC durante cleanup
            }

            IsScanning = false;
        }

        private async void OnIdentityVerified(object? sender, VerifiedIdentity verifiedIdentity)
        {
            try
            {
                ScanStatus = $"Identidad verificada: {verifiedIdentity.FullName}";

                if (_currentContext == null)
                {
                    await EnsureNfcStoppedAsync();
                    await Shell.Current.DisplayAlertAsync("Error", "No hay contexto de recogida activo", "OK");
                    return;
                }

                var authResult = await _pickupService.CheckAuthorizationAsync(_currentContext, verifiedIdentity);

                if (!authResult.IsAuthorized)
                {
                    IsScanning = false;
                    ScanStatus = $"Acceso denegado: {authResult.DenialReason}";
                    await EnsureNfcStoppedAsync();
                    await Shell.Current.DisplayAlertAsync("Acceso denegado", authResult.DenialReason ?? "No autorizado", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                var teacherId = Auth.CurrentUserId!;
                var log = await _pickupService.ConfirmPickupAsync(teacherId, _currentContext, verifiedIdentity);

                IsScanning = false;
                ScanStatus = "Recogida confirmada correctamente";
                await EnsureNfcStoppedAsync();

                await Shell.Current.DisplayAlertAsync(
                    "Recogida autorizada",
                    $"{verifiedIdentity.FullName} puede recoger a {ChildName}",
                    "OK");

                await Shell.Current.GoToAsync("../..");
            }
            catch (Exception ex)
            {
                IsScanning = false;
                ScanStatus = $"Error: {ex.Message}";
                await EnsureNfcStoppedAsync();
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
    }
}
