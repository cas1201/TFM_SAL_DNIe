using ColeHop.Core.Services.Auth;
using ColeHop.Model.Identity;
using ColeHop.View;

namespace ColeHop
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _auth;
        private bool _isDisposed;

        public AppShell(IAuthService auth)
        {
            InitializeComponent();
            _auth = auth;

            RegisterRoutes();
            _auth.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            _auth.AuthenticationStateChanged += OnAuthenticationStateChanged;
            InitializeShell();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler == null)
            {
                _isDisposed = true;
                _auth.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            }
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
                    if (!_isDisposed)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (!_isDisposed && _auth.CurrentRole.HasValue)
                            {
                                ConfigureShellForRole(_auth.CurrentRole.Value);
                            }
                        });
                    }
                }
            });
        }

        private void OnAuthenticationStateChanged(object? sender, UserRole? role)
        {
            if (_isDisposed || Shell.Current != this)
                return;

            if (role.HasValue)
            {
                ConfigureShellForRole(role.Value);
            }
            else
            {
                ShowLoginPage();
            }
        }

        private void ShowLoginPage()
        {
            if (_isDisposed)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed)
                    return;

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
            if (_isDisposed)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed)
                    return;

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
            Routing.RegisterRoute("settings", typeof(SettingsPage));
            Routing.RegisterRoute("child/manage", typeof(ChildrenPage));
            Routing.RegisterRoute("authorized/manage", typeof(AuthorizedPersonPage));
            Routing.RegisterRoute("authorization/manage", typeof(AuthorizationPage));
            Routing.RegisterRoute("addchild", typeof(AddChildPage));
            Routing.RegisterRoute("addauthorizedperson", typeof(AddAuthorizedPersonPage));
            Routing.RegisterRoute("childdetail", typeof(ChildDetailPage));
            Routing.RegisterRoute("authorizedpersondetail", typeof(AuthorizedPersonDetailPage));
            Routing.RegisterRoute("nfc/scan", typeof(NfcScanPage));
            Routing.RegisterRoute("pickup/list", typeof(DailyPickupListPage));
            Routing.RegisterRoute("pendingapprovals", typeof(PendingApprovalsPage));
            Routing.RegisterRoute("rejectreason", typeof(RejectReasonPage));
        }
    }
}
