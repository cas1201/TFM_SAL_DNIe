using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class AuthorizationPage : ContentPage
{
    private readonly AuthorizationViewModel _viewModel;

    public AuthorizationPage(AuthorizationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}