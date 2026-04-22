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
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            if (await _auth.TryRestoreSessionAsync())
                ConfigureShellForRole(_auth.CurrentRole!.Value);
            else
                await GoToAsync("//login");
        }

        private void ConfigureShellForRole(UserRole role)
        {
            Items.Clear();
            switch (role)
            {
                case UserRole.Tutor:
                    Items.Add(CreateShellContent("dashboard/tutor", typeof(DashboardTutorPage)));
                    break;
                case UserRole.Teacher:
                    Items.Add(CreateShellContent("dashboard/teacher", typeof(DashboardTeacherPage)));
                    break;
            }
        }

        private static ShellContent CreateShellContent(string route, Type pageType)
        {
            return new ShellContent
            {
                Route = route,
                ContentTemplate = new DataTemplate(pageType)
            };
        }

        private static void RegisterRoutes()
        {
            // Signup
            Routing.RegisterRoute("signup", typeof(SignupPage));

            // Tutor
            Routing.RegisterRoute("child/manage", typeof(ChildrenPage));
            Routing.RegisterRoute("authorized/manage", typeof(AuthorizedPersonPage));
            Routing.RegisterRoute("authorization/manage", typeof(AuthorizationPage));

            // Teacher
            Routing.RegisterRoute("nfc/scan", typeof(NfcScanPage));
            Routing.RegisterRoute("pickup/list", typeof(DailyPickupListPage));
        }
    }
}
