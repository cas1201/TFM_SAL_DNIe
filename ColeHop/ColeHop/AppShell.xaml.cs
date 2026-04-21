using ColeHop.View;

namespace ColeHop
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Tutor
            Routing.RegisterRoute("child/manage", typeof(ChildrenPage));
            Routing.RegisterRoute("authorized/manage", typeof(AuthorizedPersonPage));
            Routing.RegisterRoute("authorization/new", typeof(AuthorizationPage));

            // Teacher
            Routing.RegisterRoute("nfc/scan", typeof(NfcScanPage));
            Routing.RegisterRoute("pickup/list", typeof(DailyPickupListPage));

            // Auth
            Routing.RegisterRoute("signup", typeof(SignupPage));
        }
    }
}
