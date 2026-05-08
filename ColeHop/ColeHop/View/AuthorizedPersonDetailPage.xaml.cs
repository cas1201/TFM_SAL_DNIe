using ColeHop.ViewModel;

namespace ColeHop.View
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
