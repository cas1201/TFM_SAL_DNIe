using ColeHop.Core.Services.Auth;
using ColeHop.Model.Identity;
using ColeHop.View;

namespace ColeHop
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _auth;

        public AppShell(IAuthService auth)
        {
            InitializeComponent();
            _auth = auth;

            RegisterRoutes();
            _auth.AuthenticationStateChanged += OnAuthenticationStateChanged;
            InitializeShell();
        }

        private void InitializeShell()
        {
            // Por defecto mostrar LoginPage
            ShowLoginPage();

            // Luego intentar restaurar sesión en background
            Task.Run(async () =>
            {
                if (await _auth.TryRestoreSessionAsync())
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ConfigureShellForRole(_auth.CurrentRole!.Value);
                    });
                }
            });
        }

        private void OnAuthenticationStateChanged(object? sender, UserRole? role)
        {
            if (role.HasValue)
            {
                // Mostrar dashboard según rol si esta logueado el usuario
                ConfigureShellForRole(role.Value);
            }
            else
            {
                // Logout o sesión expirada
                ShowLoginPage();
            }
        }

        private void ShowLoginPage()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Items.Clear();
                var loginContent = new ShellContent
                {
                    Route = "login",
                    ContentTemplate = new DataTemplate(typeof(LoginPage))
                };
                Items.Add(loginContent);
                CurrentItem = loginContent;
            });
        }

        private void ConfigureShellForRole(UserRole role)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Items.Clear();

                switch (role)
                {
                    case UserRole.Tutor:
                        var tutorContent = new ShellContent
                        {
                            Route = "tutor",
                            ContentTemplate = new DataTemplate(typeof(DashboardTutorPage))
                        };
                        Items.Add(tutorContent);
                        CurrentItem = tutorContent;
                        break;

                    case UserRole.Teacher:
                        var teacherContent = new ShellContent
                        {
                            Route = "teacher",
                            ContentTemplate = new DataTemplate(typeof(DashboardTeacherPage))
                        };
                        Items.Add(teacherContent);
                        CurrentItem = teacherContent;
                        break;
                }
            });
        }

        private static void RegisterRoutes()
        {
            Routing.RegisterRoute("signup", typeof(SignupPage));
            Routing.RegisterRoute("child/manage", typeof(ChildrenPage));
            Routing.RegisterRoute("authorized/manage", typeof(AuthorizedPersonPage));
            Routing.RegisterRoute("authorization/manage", typeof(AuthorizationPage));
            Routing.RegisterRoute("nfc/scan", typeof(NfcScanPage));
            Routing.RegisterRoute("pickup/list", typeof(DailyPickupListPage));
        }
    }
}
