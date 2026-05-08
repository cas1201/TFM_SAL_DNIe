using ColeHop.ViewModel;

namespace ColeHop.View
{
    public partial class ChildDetailPage : ContentPage
    {
        private readonly ChildDetailViewModel _viewModel;

        public ChildDetailPage(ChildDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
}
