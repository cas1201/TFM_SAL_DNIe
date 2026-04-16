using ColeHop.Platforms.Android;
using ColeHop.Services.NFC;
using CommunityToolkit.Maui;

namespace ColeHop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            builder.Services.AddSingleton<AndroidNfcService>();
            builder.Services.AddSingleton<INfcService>(sp => sp.GetRequiredService<AndroidNfcService>());
            builder.Services.AddSingleton<INfcPlatformService>(sp => sp.GetRequiredService<AndroidNfcService>());
#endif

            return builder.Build();
        }
    }
}
