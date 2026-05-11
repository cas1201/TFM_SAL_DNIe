using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewmodel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}