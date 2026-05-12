using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using ColeHop.Platforms.Android.Nfc;

namespace ColeHop
{
    [Activity(Theme = "@style/ColeHopSplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public sealed class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop && Build.VERSION.SdkInt <= BuildVersionCodes.Q)
            {
#pragma warning disable CA1422
                Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#4361EE"));
#pragma warning restore CA1422
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(30) && !OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var controller = Window?.InsetsController;
                if (controller != null)
                {
                    controller.SetSystemBarsAppearance((int)WindowInsetsControllerAppearance.LightStatusBars, (int)WindowInsetsControllerAppearance.LightStatusBars);
                }
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent == null) return;

            var handler = IPlatformApplication.Current?.Services.GetService<AndroidNfcPlatformService>();
            handler?.HandleIntent(intent);
        }
    }
}