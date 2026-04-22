using ColeHop.Core.Services.Auth;
using ColeHop.Core.Services.Nfc;
using ColeHop.Services.Nfc;
using ColeHop.Services.Auth;


#if ANDROID
using ColeHop.Platforms.Android.Nfc;
#elif IOS
using ColeHop.Platforms.iOS.Nfc;
#endif

namespace ColeHop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            #region DI
            //Auth
            builder.Services.AddSingleton<IAuthService, AuthService>();

            // Nfc
            builder.Services.AddSingleton<NfcService>();
            builder.Services.AddSingleton<INfcService>(sp => sp.GetRequiredService<NfcService>());
#if ANDROID
            builder.Services.AddSingleton<AndroidNfcPlatformService>();
            builder.Services.AddSingleton<INfcPlatformService>(sp => sp.GetRequiredService<AndroidNfcPlatformService>());
#elif IOS
#endif
            #endregion

            return builder.Build();
        }
    }
}
