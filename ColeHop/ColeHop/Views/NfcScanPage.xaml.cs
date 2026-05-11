using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class NfcScanPage : ContentPage
{
    private readonly NfcScanViewModel _viewModel;

    public NfcScanPage(NfcScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.CleanupAsync();
    }
}