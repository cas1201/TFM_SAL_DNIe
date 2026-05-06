using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class DashboardTutorPage : ContentPage
{
    public DashboardTutorPage(DashboardTutorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}