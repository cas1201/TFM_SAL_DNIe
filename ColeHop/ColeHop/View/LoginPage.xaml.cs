using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewmodel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}