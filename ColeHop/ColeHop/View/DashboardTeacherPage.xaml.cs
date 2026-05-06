using ColeHop.ViewModel;

namespace ColeHop.View;

public partial class DashboardTeacherPage : ContentPage
{
    public DashboardTeacherPage(DashboardTeacherViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}