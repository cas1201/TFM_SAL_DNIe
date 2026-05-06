using ColeHop.Core.Services.Auth;
using ColeHop.Core.Services.Nfc;
using ColeHop.Core.Services.Pickup;
using ColeHop.Services.Auth;
using ColeHop.Services.Nfc;
using ColeHop.Services.Pickup;
using ColeHop.View;
using ColeHop.ViewModel;

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

            #region Services
            // Auth
            builder.Services.AddSingleton<IAuthService, AuthService>();

            // Shell
            builder.Services.AddSingleton<AppShell>();

            // Pickup
            builder.Services.AddSingleton<IPickupService, PickupService>();

            // Nfc
            builder.Services.AddSingleton<NfcService>();
            builder.Services.AddSingleton<INfcService>(sp => sp.GetRequiredService<NfcService>());
#if ANDROID
            builder.Services.AddSingleton<AndroidNfcPlatformService>();
            builder.Services.AddSingleton<INfcPlatformService>(sp => sp.GetRequiredService<AndroidNfcPlatformService>());
#elif IOS
            builder.Services.AddSingleton<IosNfcPlatformService>();
            builder.Services.AddSingleton<INfcPlatformService>(sp => sp.GetRequiredService<IosNfcPlatformService>());
#endif
            #endregion

            #region ViewModels
            // Auth
            builder.Services.AddTransient<LoginViewmodel>();
            builder.Services.AddTransient<SignupViewModel>();

            // Tutor
            builder.Services.AddTransient<DashboardTutorViewModel>();
            builder.Services.AddTransient<ChildrenViewModel>();
            builder.Services.AddTransient<AuthorizedPersonViewModel>();
            builder.Services.AddTransient<AuthorizationViewModel>();

            // Teacher
            builder.Services.AddTransient<DashboardTeacherViewModel>();
            builder.Services.AddTransient<DailyPickupListViewModel>();
            builder.Services.AddTransient<NfcScanViewModel>();
            #endregion

            #region Views
            // Auth
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<SignupPage>();

            // Tutor
            builder.Services.AddTransient<DashboardTutorPage>();
            builder.Services.AddTransient<ChildrenPage>();
            builder.Services.AddTransient<AuthorizedPersonPage>();
            builder.Services.AddTransient<AuthorizationPage>();

            // Teacher
            builder.Services.AddTransient<DashboardTeacherPage>();
            builder.Services.AddTransient<DailyPickupListPage>();
            builder.Services.AddTransient<NfcScanPage>();
            #endregion

            return builder.Build();
        }
    }
}
