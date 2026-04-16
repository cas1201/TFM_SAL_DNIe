#if ANDROID
using Android.App;
using Android.Nfc;
using Android.Content;
using ColeHop.Services.NFC;

namespace ColeHop.Platforms.Android
{
    public class AndroidNfcService : INfcService, INfcPlatformService
    {
        readonly Activity _activity;
        readonly NfcAdapter? _adapter;

        public event EventHandler<NfcScanResult>? TagDetected;

        public AndroidNfcService()
        {
            _activity = Platform.CurrentActivity!;
            _adapter = NfcAdapter.GetDefaultAdapter(_activity);
        }

        public bool IsSupported => _adapter != null;
        public bool IsEnabled => _adapter?.IsEnabled == true;

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public void HandlePlatformIntent(object intent)
        {
            if (intent is not Intent androidIntent)
                return;

            if (androidIntent.Action != NfcAdapter.ActionTagDiscovered &&
                androidIntent.Action != NfcAdapter.ActionNdefDiscovered)
                return;

            var tag = androidIntent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null)
                return;

            var uid = BitConverter
                .ToString(tag.GetId())
                .Replace("-", "");

            TagDetected?.Invoke(this, new NfcScanResult
            {
                Uid = uid
            });
        }
    }
#endif
}
