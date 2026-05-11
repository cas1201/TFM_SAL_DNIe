using ColeHop.ViewModels;

namespace ColeHop.Views
{
    public partial class AddAuthorizedPersonPage : ContentPage
    {
        public AddAuthorizedPersonPage(AddAuthorizedPersonViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}