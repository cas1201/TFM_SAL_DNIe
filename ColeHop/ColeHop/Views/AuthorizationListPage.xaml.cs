using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class AuthorizationListPage : ContentPage
{
    private readonly AuthorizationListViewModel _viewModel;

    public AuthorizationListPage(AuthorizationListViewModel viewModel)
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
