using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class RejectReasonPage : ContentPage
{
    public RejectReasonPage(RejectReasonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
