using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class PendingApprovalsPage : ContentPage
{
    private readonly PendingApprovalsViewModel _viewModel;

    public PendingApprovalsPage(PendingApprovalsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
