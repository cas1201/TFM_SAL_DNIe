using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class RejectReasonPage : ContentPage
{
    public RejectReasonPage(RejectReasonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
