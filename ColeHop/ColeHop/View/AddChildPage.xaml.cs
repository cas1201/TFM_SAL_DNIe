namespace ColeHop.View
{
    public partial class AddChildPage : ContentPage
    {
        public AddChildPage(ViewModel.AddChildViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
