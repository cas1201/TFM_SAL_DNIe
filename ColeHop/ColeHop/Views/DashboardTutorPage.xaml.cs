using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class DashboardTutorPage : ContentPage
{
    public DashboardTutorPage(DashboardTutorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}