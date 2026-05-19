using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using ColeHop.Services.Nfc;

namespace ColeHop.Platforms.Android.Nfc
{
    public sealed class AndroidNfcPlatformService : Java.Lang.Object, INfcPlatformService
    {
        private readonly Context _context;
        private readonly NfcAdapter? _nfcAdapter;
        private IsoDep? _isoDep;
        private bool _isListening;
        private TaskCompletionSource<bool>? _tagDetectedTcs;

        public AndroidNfcPlatformService()
        {
            // Obtener contexto de Android desde Platform API
            _context = Platform.CurrentActivity?.ApplicationContext ?? Platform.AppContext;

            if (_context == null)
                throw new InvalidOperationException("No se pudo obtener el contexto de Android");

            _nfcAdapter = NfcAdapter.GetDefaultAdapter(_context);
        }

        public bool IsSupported => _nfcAdapter != null;

        public bool IsEnabled => _nfcAdapter?.IsEnabled ?? false;

        public Task StartListeningAsync()
        {
            if (_nfcAdapter == null)
                return Task.CompletedTask;

            var activity = Platform.CurrentActivity;
            if (activity == null)
                return Task.CompletedTask;

            _isListening = true;
            _tagDetectedTcs = new TaskCompletionSource<bool>();

            // Configurar foreground dispatch para recibir intents NFC con prioridad
            var intent = new Intent(activity, activity.GetType()).AddFlags(ActivityFlags.SingleTop);

            PendingIntent? pendingIntent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
#pragma warning disable CA1416
                pendingIntent = PendingIntent.GetActivity(activity, 0, intent, PendingIntentFlags.Mutable);
#pragma warning restore CA1416
            }
            else
            {
                pendingIntent = PendingIntent.GetActivity(activity, 0, intent, PendingIntentFlags.UpdateCurrent);
            }

            // Filtros para tecnologías ISO-DEP (usadas por DNIe)
            var isoDepClassName = Java.Lang.Class.FromType(typeof(IsoDep)).Name;
            if (string.IsNullOrEmpty(isoDepClassName))
                return Task.CompletedTask;

            var techList = new[]
            {
                new[] { isoDepClassName }
            };

            // Activar foreground dispatch
            _nfcAdapter.EnableForegroundDispatch(activity, pendingIntent, null, techList);

            return Task.CompletedTask;
        }

        public Task StopListeningAsync()
        {
            _isListening = false;

            try
            {
                // Desactivar foreground dispatch
                if (_nfcAdapter != null)
                {
                    var activity = Platform.CurrentActivity;
                    if (activity != null)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("NFC: Desactivando foreground dispatch...");
                            _nfcAdapter.DisableForegroundDispatch(activity);
                            System.Diagnostics.Debug.WriteLine("NFC: Foreground dispatch desactivado");
                        }
                        catch (Java.Lang.IllegalStateException ex)
                        {
                            // La Activity puede estar en un estado incorrecto (pausada, etc.)
                            System.Diagnostics.Debug.WriteLine($"NFC: Error al desactivar foreground dispatch (IllegalStateException): {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"NFC: Error al desactivar foreground dispatch: {ex.GetType().Name} - {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("NFC: No se puede desactivar foreground dispatch - Activity es null");
                    }
                }

                // Cerrar conexión ISO-DEP si existe
                try
                {
                    if (_isoDep != null)
                    {
                        System.Diagnostics.Debug.WriteLine("NFC: Cerrando conexión ISO-DEP...");
                        _isoDep.Close();
                        System.Diagnostics.Debug.WriteLine("NFC: Conexión ISO-DEP cerrada");
                    }
                }
                catch (Java.IO.IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NFC: Error al cerrar conexión ISO-DEP: {ex.Message}");
                }
                finally
                {
                    _isoDep = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NFC: Error general en StopListeningAsync: {ex.GetType().Name} - {ex.Message}");
            }

            // Cancelar TaskCompletionSource si existe
            _tagDetectedTcs?.TrySetCanceled();
            _tagDetectedTcs = null;

            return Task.CompletedTask;
        }

        public async Task WaitForTagAsync(CancellationToken cancellationToken)
        {
            if (_tagDetectedTcs == null)
                throw new InvalidOperationException("No hay sesión de escucha activa. Llame a StartListeningAsync primero.");

            System.Diagnostics.Debug.WriteLine("NFC: Esperando detección de tag...");

            // Registrar cancelación
            using var registration = cancellationToken.Register(() =>
            {
                System.Diagnostics.Debug.WriteLine("NFC: WaitForTagAsync cancelado");
                _tagDetectedTcs?.TrySetCanceled();
            });

            try
            {
                await _tagDetectedTcs.Task;
                System.Diagnostics.Debug.WriteLine("NFC: Tag detectado y conectado");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("NFC: Espera de tag cancelada");
                throw;
            }
        }

        public void HandleIntent(Intent intent)
        {
            try
            {
                // Ignorar intents NFC si no estamos esperando una lectura
                if (!_isListening)
                {
                    System.Diagnostics.Debug.WriteLine("NFC: Intent ignorado - no hay sesión de lectura activa");
                    return;
                }

                if (intent.Action != NfcAdapter.ActionTechDiscovered &&
                    intent.Action != NfcAdapter.ActionTagDiscovered)
                {
                    System.Diagnostics.Debug.WriteLine($"NFC: Intent ignorado - action no soportada: {intent.Action}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"NFC: Procesando intent - Action: {intent.Action}");

                // Extraer tag del intent - manejo según versión de Android
                Tag? tag;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    // Android 13+ (API 33+)
#pragma warning disable CA1416
                    tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag, Java.Lang.Class.FromType(typeof(Tag))) as Tag;
#pragma warning restore CA1416
                }
                else
                {
                    // Android 12 y anteriores
#pragma warning disable CA1422
                    tag = (Tag?)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
#pragma warning restore CA1422
                }

                if (tag == null)
                {
                    System.Diagnostics.Debug.WriteLine("NFC: Tag es null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"NFC: Tag detectado - ID: {BitConverter.ToString(tag.GetId() ?? Array.Empty<byte>())}");

                // Cerrar conexión anterior si existe
                try
                {
                    _isoDep?.Close();
                }
                catch (Java.IO.IOException)
                {
                    // Ignorar errores al cerrar conexión anterior
                }

                // Obtener tecnología ISO-DEP del tag
                _isoDep = IsoDep.Get(tag);
                if (_isoDep == null)
                {
                    System.Diagnostics.Debug.WriteLine("NFC: Tag no soporta ISO-DEP");
                    throw new InvalidOperationException("El tag NFC no es compatible con ISO-DEP.");
                }

                System.Diagnostics.Debug.WriteLine("NFC: Conectando a tag ISO-DEP...");
                _isoDep.Timeout = 5000; // 5 segundos para operaciones PACE y Secure Messaging
                _isoDep.Connect();
                System.Diagnostics.Debug.WriteLine($"NFC: Conectado exitosamente (timeout: {_isoDep.Timeout}ms)");

                // Señalar que el tag fue detectado y conectado
                _tagDetectedTcs?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NFC: Error en HandleIntent - {ex.GetType().Name}: {ex.Message}");
                _isoDep?.Close();
                _isoDep = null;

                // Señalar error en la detección
                _tagDetectedTcs?.TrySetException(ex);
                throw;
            }
        }

        public async Task<NfcScanResult> TransceiveAsync(byte[] apdu, CancellationToken cancellationToken)
        {
            if (_isoDep == null || !_isoDep.IsConnected)
                return new NfcScanResult(Array.Empty<byte>(), false, "No hay comunicación NFC activa.");

            try
            {
                var response = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return _isoDep.Transceive(apdu);
                }, cancellationToken);

                if (response == null)
                    return new NfcScanResult(Array.Empty<byte>(), false, "Respuesta NFC vacía.");

                return new NfcScanResult(response, true, null);
            }
            catch (Exception ex)
            {
                return new NfcScanResult(null, false, ex.Message);
            }
        }
    }
}