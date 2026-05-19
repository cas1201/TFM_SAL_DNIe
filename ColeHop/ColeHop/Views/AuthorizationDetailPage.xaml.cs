using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class AuthorizationDetailPage : ContentPage
{
    private readonly AuthorizationDetailViewModel _viewModel;

    public AuthorizationDetailPage(AuthorizationDetailViewModel viewModel)
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
