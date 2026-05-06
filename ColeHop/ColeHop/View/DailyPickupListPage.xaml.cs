using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class DailyPickupListPage : ContentPage
{
    private readonly DailyPickupListViewModel _viewModel;

    public DailyPickupListPage(DailyPickupListViewModel viewModel)
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