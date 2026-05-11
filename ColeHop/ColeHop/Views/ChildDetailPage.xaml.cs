using ColeHop.ViewModels;

namespace ColeHop.Views
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
