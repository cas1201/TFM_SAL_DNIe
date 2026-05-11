using ColeHop.ViewModels;

namespace ColeHop.Views
{
    public partial class AuthorizedPersonDetailPage : ContentPage
    {
        private readonly AuthorizedPersonDetailViewModel _viewModel;

        public AuthorizedPersonDetailPage(AuthorizedPersonDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
}
