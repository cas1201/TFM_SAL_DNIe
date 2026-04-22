using ColeHop.Core.Services.Auth;

namespace ColeHop
{
    public partial class App : Application
    {
        private readonly IAuthService _auth;

        public App(IAuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell(_auth));
        }
    }
}