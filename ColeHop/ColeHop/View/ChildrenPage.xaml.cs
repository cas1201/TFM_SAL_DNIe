using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class ChildrenPage : ContentPage
{
    private readonly ChildrenViewModel _viewModel;

    public ChildrenPage(ChildrenViewModel viewModel)
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