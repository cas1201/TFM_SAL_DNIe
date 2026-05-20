using ColeHop.Resources.Strings;
using System.Globalization;

namespace ColeHop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Aplicar idioma guardado
            ApplyLanguage(Preferences.Get("app_language", "es"));

            var theme = Preferences.Get("app_theme", "System");
            UserAppTheme = theme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var appShell = Handler?.MauiContext?.Services.GetRequiredService<AppShell>();
            return new Window(appShell!);
        }

        /// <summary>
        /// Aplica el idioma cambiando la cultura global y forzando al ResourceManager a releer.
        /// </summary>
        public static void ApplyLanguage(string languageCode)
        {
            var culture = new CultureInfo(languageCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Forzar al ResourceManager a usar la nueva cultura
            AppResources.Culture = culture;
        }

        /// <summary>
        /// Recrea toda la UI de la aplicación con una nueva instancia de AppShell.
        /// Llamar después de cambiar el idioma para que toda la interfaz se reconstruya.
        /// </summary>
        public static void RestartUI()
        {
            if (Current is not App app)
                return;

            var window = app.Windows.FirstOrDefault();
            if (window == null)
                return;

            var services = window.Handler?.MauiContext?.Services
                           ?? app.Handler?.MauiContext?.Services;

            if (services == null)
                return;

            var newShell = services.GetRequiredService<AppShell>();
            window.Page = newShell;
        }
    }
}