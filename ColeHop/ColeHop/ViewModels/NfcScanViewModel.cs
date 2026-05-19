using ColeHop.Resources.Strings;
using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.Nfc;
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
        private int _failedAttempts;
        private const int MaxAttempts = 5;
        private const int BaseLockoutSeconds = 5;
        private bool _isNavigatingAway;
        private bool _userCancelled;
        private bool _identityHandled;

        public bool IsNavigatingAway => _isNavigatingAway;

        [ObservableProperty]
        private string _childId = string.Empty;

        [ObservableProperty]
        private string _childName = string.Empty;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _scanStatus = AppResources.WaitingForDni;

        [ObservableProperty]
        private string _can = string.Empty;

        [ObservableProperty]
        private bool _showCanInput = true;

        [ObservableProperty]
        private bool _dniDetected;

        public NfcScanViewModel(IAuthService auth, IAlertService alertService, INfcService nfcService, IPickupService pickupService) : base(auth, alertService)
        {
            _nfcService = nfcService;
            _pickupService = pickupService;
        }

        public async Task InitializeAsync()
        {
            if (_isNavigatingAway)
                return;

            _identityHandled = false;
            _nfcService.IdentityVerified -= OnIdentityVerified;
            _nfcService.IdentityVerified += OnIdentityVerified;

            if (!_nfcService.IsSupported)
            {
                _isNavigatingAway = true;
                ScanStatus = AppResources.NfcNotSupportedDevice;
                await Alert.ShowAsync(AppResources.Error, AppResources.NfcNotSupported);
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (!_nfcService.IsEnabled)
            {
                _isNavigatingAway = true;
                ScanStatus = AppResources.EnableNfcInSettings;
                await Alert.ShowAsync(AppResources.Warning, AppResources.NfcDisabledWarning);
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (string.IsNullOrEmpty(ChildId))
            {
                _isNavigatingAway = true;
                await Alert.ShowAsync(AppResources.Error, AppResources.ChildNotSpecified);
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                var teacherId = Auth.CurrentUserId!;
                var today = DateOnly.FromDateTime(DateTime.Today);
                _currentContext = await _pickupService.StartPickupAsync(teacherId, ChildId, today);
                ScanStatus = string.Format(AppResources.ReadyToScanForChild, ChildName);
            }
            catch (Exception ex)
            {
                _isNavigatingAway = true;
                ScanStatus = AppResources.ErrorStartingPickup;
                await Alert.ShowAsync(AppResources.Error, ex.Message);
                await Shell.Current.GoToAsync("..");
            }
        }

        [RelayCommand]
        private async Task StartScanAsync()
        {
            if (string.IsNullOrWhiteSpace(Can) || Can.Length != 6)
            {
                await Alert.ShowAsync(AppResources.Error, AppResources.CanMustBe6Digits);
                return;
            }

            if (_currentContext == null)
            {
                await Alert.ShowAsync(AppResources.Error, AppResources.InvalidPickupContext);
                return;
            }

            try
            {
                if (_failedAttempts >= MaxAttempts)
                {
                    var lockoutSeconds = BaseLockoutSeconds * (_failedAttempts - MaxAttempts + 1);
                    ScanStatus = string.Format(AppResources.TooManyAttemptsWait, lockoutSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(lockoutSeconds));
                }

                IsScanning = true;
                ShowCanInput = false;
                DniDetected = false;
                _userCancelled = false;
                ScanStatus = AppResources.ApproachDniToReader;

                _scanCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var progress = new Progress<string>(phase =>
                {
                    ScanStatus = phase;
                    if (phase.Contains("DNI detectado", StringComparison.OrdinalIgnoreCase))
                        DniDetected = true;
                });
                await _nfcService.BeginDnieReadingAsync(Can, _scanCts.Token, progress);

                _failedAttempts = 0;
            }
            catch (OperationCanceledException)
            {
                if (_userCancelled || _isNavigatingAway)
                {
                    // Cancelación por el usuario o navegación hacia atrás: no mostrar error
                    ScanStatus = "Escaneo cancelado";
                    await EnsureNfcStoppedAsync();
                }
                else
                {
                    // Timeout real
                    _failedAttempts++;
                    IsScanning = false;
                    ShowCanInput = true;
                    ScanStatus = "Tiempo de espera agotado. Intente de nuevo.";
                    await EnsureNfcStoppedAsync();
                    await Alert.ShowAsync(
                        AppResources.TimeoutExpired,
                        AppResources.DniNotDetectedTimeout);
                }
            }
            catch (Exception ex)
            {
                _failedAttempts++;
                IsScanning = false;
                ShowCanInput = true;
                var friendlyMessage = GetFriendlyErrorMessage(ex);
                ScanStatus = $"Error: {friendlyMessage}";
                await EnsureNfcStoppedAsync();
                await Alert.ShowAsync(AppResources.VerificationError, friendlyMessage);
            }
        }

        [RelayCommand]
        private async Task CancelScanAsync()
        {
            _userCancelled = true;
            _scanCts?.Cancel();
            IsScanning = false;
            ShowCanInput = true;
            ScanStatus = "Escaneo cancelado";
            await EnsureNfcStoppedAsync();
        }

        private static string GetFriendlyErrorMessage(Exception ex)
        {
            var message = ex.Message ?? string.Empty;

            // PACE errors - typically CAN related
            if (message.Contains("SW=6988", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("SW=6982", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("SW=6300", StringComparison.OrdinalIgnoreCase))
                return "El código CAN introducido no es correcto. Revise los 6 dígitos que aparecen en la parte inferior derecha del anverso de su DNI.";

            if (message.Contains("SW=6A82", StringComparison.OrdinalIgnoreCase))
                return "No se ha podido acceder a los datos del DNI. Asegúrese de que el documento es un DNI electrónico válido.";

            if (message.Contains("SW=6999", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("SW=6F00", StringComparison.OrdinalIgnoreCase))
                return "Se ha producido un error de comunicación con el DNI. Vuelva a acercar el documento y manténgalo quieto durante la lectura.";

            if (message.Contains("SW=", StringComparison.OrdinalIgnoreCase) &&
                message.Contains("PACE", StringComparison.OrdinalIgnoreCase))
                return "Error al establecer la conexión segura con el DNI. Verifique que el CAN es correcto y vuelva a intentarlo.";

            if (message.Contains("SW=", StringComparison.OrdinalIgnoreCase))
                return "Error de comunicación con el DNI electrónico. Mantenga el documento pegado al teléfono sin moverlo e inténtelo de nuevo.";

            // NFC / IO errors
            if (message.Contains("TagLost", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Tag was lost", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("IOException", StringComparison.OrdinalIgnoreCase))
                return "Se ha perdido la conexión con el DNI. Mantenga el documento pegado al teléfono sin moverlo durante toda la lectura.";

            if (message.Contains("transceive", StringComparison.OrdinalIgnoreCase))
                return "Error de comunicación NFC. Asegúrese de que el DNI está bien posicionado sobre el lector NFC del teléfono.";

            // BouncyCastle / crypto errors
            if (message.Contains("Org.BouncyCastle", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("cipher", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("mac", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("decrypt", StringComparison.OrdinalIgnoreCase) ||
                ex.GetType().FullName?.Contains("BouncyCastle") == true)
                return "Error al verificar la seguridad del documento. El CAN podría ser incorrecto o el DNI se movió durante la lectura. Inténtelo de nuevo.";

            // Secure messaging
            if (message.Contains("SecureMessaging", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Respuesta NFC demasiado corta", StringComparison.OrdinalIgnoreCase))
                return "La comunicación segura con el DNI se ha interrumpido. Vuelva a acercar el documento e inténtelo de nuevo.";

            // Generic NFC
            if (message.Contains("NFC", StringComparison.OrdinalIgnoreCase))
                return "Error en la lectura NFC. Asegúrese de que el DNI está bien posicionado y no lo mueva durante el proceso.";

            // Fallback
            return "Se ha producido un error al leer el DNI. Compruebe que el CAN es correcto, mantenga el documento quieto sobre el teléfono e inténtelo de nuevo.";
        }

        private async Task EnsureNfcStoppedAsync()
        {
            try
            {
                await _nfcService.StopAsync();
            }
            catch
            {
            }
        }

        public async Task CleanupAsync()
        {
            _userCancelled = true;
            _nfcService.IdentityVerified -= OnIdentityVerified;
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;

            // Limpiar CAN de memoria
            Can = string.Empty;

            if (!_isNavigatingAway)
            {
                try
                {
                    await _nfcService.StopAsync();
                }
                catch
                {
                }
            }

            IsScanning = false;
        }

        private async void OnIdentityVerified(object? sender, VerifiedIdentity verifiedIdentity)
        {
            // Evitar múltiples ejecuciones
            if (_identityHandled || _isNavigatingAway)
                return;
            _identityHandled = true;

            // Desuscribir inmediatamente para evitar re-entradas
            _nfcService.IdentityVerified -= OnIdentityVerified;

            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(async () => await HandleIdentityVerifiedAsync(verifiedIdentity));
                return;
            }

            await HandleIdentityVerifiedAsync(verifiedIdentity);
        }

        private async Task HandleIdentityVerifiedAsync(VerifiedIdentity verifiedIdentity)
        {
            try
            {
                ScanStatus = $"Identidad verificada: {verifiedIdentity.FullName}";
                IsScanning = false;
                await EnsureNfcStoppedAsync();

                if (_currentContext == null)
                {
                    await Alert.ShowAsync(AppResources.Error, AppResources.NoActivePickupContext);
                    return;
                }

                var authResult = await _pickupService.CheckAuthorizationAsync(_currentContext, verifiedIdentity);

                _isNavigatingAway = true;

                if (!authResult.IsAuthorized)
                {
                    ScanStatus = $"Acceso denegado: {authResult.DenialReason}";
                    await Alert.ShowAsync("Recogida no autorizada", authResult.DenialReason ?? AppResources.NotAuthorized);
                }
                else
                {
                    var teacherId = Auth.CurrentUserId!;
                    await _pickupService.ConfirmPickupAsync(teacherId, _currentContext, verifiedIdentity);
                    ScanStatus = "Recogida confirmada correctamente";
                    await Alert.ShowAsync(
                        AppResources.PickupAuthorized,
                        $"{verifiedIdentity.GivenNames} {verifiedIdentity.Surnames} con DNI {verifiedIdentity.DocumentNumber} puede recoger a {ChildName}");
                }

                // Navegar de vuelta
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                IsScanning = false;
                ScanStatus = $"Error: {ex.Message}";
                await EnsureNfcStoppedAsync();
                await Alert.ShowAsync(AppResources.Error, ex.Message);
            }
        }
    }
}
