namespace ColeHop.View
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(ViewModel.SettingsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
