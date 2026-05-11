using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class DashboardTeacherPage : ContentPage
{
    public DashboardTeacherPage(DashboardTeacherViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}