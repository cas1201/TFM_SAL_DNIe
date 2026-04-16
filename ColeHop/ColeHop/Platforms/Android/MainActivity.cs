using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using ColeHop.Services.NFC;

namespace ColeHop
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTagDiscovered], Categories = [Intent.CategoryDefault])]
    public class MainActivity : MauiAppCompatActivity
    {
        private NfcAdapter? nfcAdapter;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            if (intent == null) return;
            base.OnNewIntent(intent);

            var handler = IPlatformApplication.Current?.Services.GetService<INfcPlatformService>();
            handler?.HandlePlatformIntent(intent);
        }

    }
}