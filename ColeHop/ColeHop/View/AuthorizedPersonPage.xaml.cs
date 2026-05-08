using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class AuthorizedPersonPage : ContentPage
{
    private readonly AuthorizedPersonViewModel _viewModel;

    public AuthorizedPersonPage(AuthorizedPersonViewModel viewModel)
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
