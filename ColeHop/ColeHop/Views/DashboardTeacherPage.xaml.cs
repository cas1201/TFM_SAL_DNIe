using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class DashboardTeacherPage : ContentPage
{
    public DashboardTeacherPage(DashboardTeacherViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity?.Window?.DecorView?.RootWindowInsets != null)
        {
            var insets = activity.Window.DecorView.RootWindowInsets.GetInsets(Android.Views.WindowInsets.Type.StatusBars());
            var density = DeviceDisplay.MainDisplayInfo.Density;
            var topPadding = insets.Top / density;
            Padding = new Thickness(0, topPadding, 0, 0);
        }
#endif
    }
}