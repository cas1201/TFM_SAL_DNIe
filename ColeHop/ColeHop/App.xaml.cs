namespace ColeHop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

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
            // Resolver AppShell desde el contenedor DI para evitar problemas con servicios disposed
            var appShell = Handler?.MauiContext?.Services.GetRequiredService<AppShell>();
            return new Window(appShell!);
        }
    }
}