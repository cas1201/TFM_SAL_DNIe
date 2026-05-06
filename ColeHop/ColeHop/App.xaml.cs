namespace ColeHop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Resolver AppShell desde el contenedor DI para evitar problemas con servicios disposed
            var appShell = Handler?.MauiContext?.Services.GetRequiredService<AppShell>();
            return new Window(appShell!);
        }
    }
}