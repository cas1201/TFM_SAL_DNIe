using ColeHop.ViewModels;

namespace ColeHop.Views
{
    public partial class AddChildPage : ContentPage
    {
        public AddChildPage(AddChildViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
