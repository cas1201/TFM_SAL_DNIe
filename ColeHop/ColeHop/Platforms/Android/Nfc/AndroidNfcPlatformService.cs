using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using ColeHop.Services.Nfc;

namespace ColeHop.Platforms.Android.Nfc
{
    public sealed class AndroidNfcPlatformService : Java.Lang.Object, INfcPlatformService
    {
        private readonly Context _context;
        private readonly NfcAdapter? _nfcAdapter;

        private IsoDep? _isoDep;

        public AndroidNfcPlatformService(Context context)
        {
            _context = context;
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(context);
        }

        public bool IsSupported => _nfcAdapter != null;

        public bool IsEnabled => _nfcAdapter?.IsEnabled ?? false;

        public Task StartListeningAsync()
        {
            // La escucha real se gestiona por Intents de Android.
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

            var tag = (Tag?)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
            if (tag == null)
                return;

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