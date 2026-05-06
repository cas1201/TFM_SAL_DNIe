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
            return Task.CompletedTask;
        }

        public Task StopListeningAsync()
        {
            try
            {
                _isoDep?.Close();
                _isoDep = null;
            }
            catch { }

            return Task.CompletedTask;
        }

        public void HandleIntent(Intent intent) 
        {
            if (intent.Action != NfcAdapter.ActionTechDiscovered &&
                intent.Action != NfcAdapter.ActionTagDiscovered)
                return;

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
                return;

            // Obtener tecnología ISO-DEP del tag
            _isoDep = IsoDep.Get(tag);
            if (_isoDep == null)
                throw new InvalidOperationException("El tag NFC no es compatible con ISO-DEP.");

            _isoDep.Connect();
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

                return new NfcScanResult(response, true, null);
            }
            catch (Exception ex)
            {
                return new NfcScanResult(null, false, ex.Message);
            }
        }
    }
}