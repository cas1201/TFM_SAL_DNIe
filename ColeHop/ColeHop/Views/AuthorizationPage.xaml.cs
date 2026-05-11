using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class AuthorizationPage : ContentPage
{
    private readonly AuthorizationViewModel _viewModel;

    public AuthorizationPage(AuthorizationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}