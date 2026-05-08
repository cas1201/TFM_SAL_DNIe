namespace ColeHop.View
{
    public partial class AddAuthorizedPersonPage : ContentPage
    {
        public AddAuthorizedPersonPage(ViewModel.AddAuthorizedPersonViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
