using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.Nfc;
using ColeHop.Services.Pickup;
using ColeHop.Services.Teacher;
using ColeHop.Services.TutorManagement;
using ColeHop.Helpers;
using ColeHop.Views;
using ColeHop.ViewModels;

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
                    fonts.AddFont("MaterialSymbolsRounded.ttf", "MaterialSymbolsRounded");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("UnderlineEntry", (handler, view) =>
                    {
                        var editText = handler.PlatformView;
                        var density = editText.Context?.Resources?.DisplayMetrics?.Density ?? 1;
                        var primaryColor = (Color)(Application.Current?.Resources["Primary"] ?? Colors.Black);
                        var androidColor = new Android.Graphics.Color(
                            (byte)(primaryColor.Red * 255),
                            (byte)(primaryColor.Green * 255),
                            (byte)(primaryColor.Blue * 255));

                        var transparent = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Transparent);
                        var line = new Android.Graphics.Drawables.GradientDrawable();
                        line.SetColor(androidColor);

                        var layers = new Android.Graphics.Drawables.LayerDrawable([transparent, line]);
                        var lineHeight = (int)System.Math.Max(1, 1.5 * density);
                        if (OperatingSystem.IsAndroidVersionAtLeast(23))
                        {
                            layers.SetLayerHeight(1, lineHeight);
                            layers.SetLayerGravity(1, Android.Views.GravityFlags.Bottom);
                        }

                        editText.Background = layers;
                    });

                    Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("UnderlinePicker", (handler, view) =>
                    {
                        var picker = handler.PlatformView;
                        var density = picker.Context?.Resources?.DisplayMetrics?.Density ?? 1;
                        var primaryColor = (Color)(Application.Current?.Resources["Primary"] ?? Colors.Black);
                        var androidColor = new Android.Graphics.Color(
                            (byte)(primaryColor.Red * 255),
                            (byte)(primaryColor.Green * 255),
                            (byte)(primaryColor.Blue * 255));

                        var transparent = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Transparent);
                        var line = new Android.Graphics.Drawables.GradientDrawable();
                        line.SetColor(androidColor);

                        var layers = new Android.Graphics.Drawables.LayerDrawable([transparent, line]);
                        var lineHeight = (int)System.Math.Max(1, 1.5 * density);
                        if (OperatingSystem.IsAndroidVersionAtLeast(23))
                        {
                            layers.SetLayerHeight(1, lineHeight);
                            layers.SetLayerGravity(1, Android.Views.GravityFlags.Bottom);
                        }

                        picker.Background = layers;
                    });

                    Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("UnderlineDatePicker", (handler, view) =>
                    {
                        var datePicker = handler.PlatformView;
                        var density = datePicker.Context?.Resources?.DisplayMetrics?.Density ?? 1;
                        var primaryColor = (Color)(Application.Current?.Resources["Primary"] ?? Colors.Black);
                        var androidColor = new Android.Graphics.Color(
                            (byte)(primaryColor.Red * 255),
                            (byte)(primaryColor.Green * 255),
                            (byte)(primaryColor.Blue * 255));

                        var transparent = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Transparent);
                        var line = new Android.Graphics.Drawables.GradientDrawable();
                        line.SetColor(androidColor);

                        var layers = new Android.Graphics.Drawables.LayerDrawable([transparent, line]);
                        var lineHeight = (int)System.Math.Max(1, 1.5 * density);
                        if (OperatingSystem.IsAndroidVersionAtLeast(23))
                        {
                            layers.SetLayerHeight(1, lineHeight);
                            layers.SetLayerGravity(1, Android.Views.GravityFlags.Bottom);
                        }

                        datePicker.Background = layers;
                    });
#endif
                });

            #region Services
            // Alert
            builder.Services.AddSingleton<IAlertService, AlertService>();

            // Auth
#if DEBUG
            builder.Services.AddSingleton<IAuthService, MockAuthService>();
#else
            builder.Services.AddSingleton<IAuthService, HttpAuthService>();
#endif

            // Shell
            builder.Services.AddTransient<AppShell>();

            // Pickup
#if DEBUG
            builder.Services.AddSingleton<IPickupService, MockPickupService>();
#else
            builder.Services.AddSingleton<IPickupService, HttpPickupService>();
#endif

            // TutorManagement
            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(ApiConfig.BaseUrl)
                };
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                return httpClient;
            });
            builder.Services.AddSingleton<JwtStorage>();

#if DEBUG
            builder.Services.AddSingleton<ITutorManagementService, MockTutorManagementService>();
            builder.Services.AddSingleton<ITeacherService, MockTeacherService>();
#else
            builder.Services.AddSingleton<ITutorManagementService, HttpTutorManagementService>();
#endif

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

            // Settings
            builder.Services.AddTransient<SettingsViewModel>();

            // Tutor
            builder.Services.AddTransient<DashboardTutorViewModel>();
            builder.Services.AddTransient<ChildrenViewModel>();
            builder.Services.AddTransient<AuthorizedPersonViewModel>();
            builder.Services.AddTransient<AuthorizationViewModel>();
            builder.Services.AddTransient<AuthorizationListViewModel>();
            builder.Services.AddTransient<AuthorizationDetailViewModel>();
            builder.Services.AddTransient<AddChildViewModel>();
            builder.Services.AddTransient<AddAuthorizedPersonViewModel>();
            builder.Services.AddTransient<ChildDetailViewModel>();
            builder.Services.AddTransient<AuthorizedPersonDetailViewModel>();

            // Teacher
            builder.Services.AddTransient<DashboardTeacherViewModel>();
            builder.Services.AddTransient<DailyPickupListViewModel>();
            builder.Services.AddTransient<NfcScanViewModel>();
            builder.Services.AddTransient<PendingApprovalsViewModel>();
            builder.Services.AddTransient<RejectReasonViewModel>();
            #endregion

            #region Views
            // Auth
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<SignupPage>();

            // Settings
            builder.Services.AddTransient<SettingsPage>();

            // Tutor
            builder.Services.AddTransient<DashboardTutorPage>();
            builder.Services.AddTransient<ChildrenPage>();
            builder.Services.AddTransient<AuthorizedPersonPage>();
            builder.Services.AddTransient<AuthorizationPage>();
            builder.Services.AddTransient<AuthorizationListPage>();
            builder.Services.AddTransient<AuthorizationDetailPage>();
            builder.Services.AddTransient<AddChildPage>();
            builder.Services.AddTransient<AddAuthorizedPersonPage>();
            builder.Services.AddTransient<ChildDetailPage>();
            builder.Services.AddTransient<AuthorizedPersonDetailPage>();

            // Teacher
            builder.Services.AddTransient<DashboardTeacherPage>();
            builder.Services.AddTransient<DailyPickupListPage>();
            builder.Services.AddTransient<NfcScanPage>();
            builder.Services.AddTransient<PendingApprovalsPage>();
            builder.Services.AddTransient<RejectReasonPage>();
            #endregion

            return builder.Build();
        }
    }
}
