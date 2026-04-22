using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using ColeHop.Platforms.Android.Nfc;

namespace ColeHop
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTagDiscovered], Categories = [Intent.CategoryDefault])]
    public sealed class MainActivity : MauiAppCompatActivity
    {
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent == null) return;

            var handler = IPlatformApplication.Current?.Services.GetService<AndroidNfcPlatformService>();
            handler?.HandleIntent(intent);
        }
    }
}