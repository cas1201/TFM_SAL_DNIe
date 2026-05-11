using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class ChildrenPage : ContentPage
{
    private readonly ChildrenViewModel _viewModel;

    public ChildrenPage(ChildrenViewModel viewModel)
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
