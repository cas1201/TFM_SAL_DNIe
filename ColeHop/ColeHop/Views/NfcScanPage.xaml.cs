using ColeHop.ViewModels;

namespace ColeHop.Views;

public partial class NfcScanPage : ContentPage
{
    private readonly NfcScanViewModel _viewModel;

    public NfcScanPage(NfcScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnStartScanClicked(object sender, EventArgs e)
    {
        // Ocultar teclado deshabilitando temporalmente el Entry
        CanEntry.IsEnabled = false;
        await Task.Delay(100);
        CanEntry.IsEnabled = true;

        // Ejecutar el comando
        if (_viewModel.StartScanCommand.CanExecute(null))
            await _viewModel.StartScanCommand.ExecuteAsync(null);
    }

    private void HideKeyboard()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        var view = activity?.CurrentFocus;
        if (view != null)
        {
            var imm = (Android.Views.InputMethods.InputMethodManager?)activity!.GetSystemService(Android.Content.Context.InputMethodService);
            imm?.HideSoftInputFromWindow(view.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
            view.ClearFocus();
        }
#elif IOS || MACCATALYST
        UIKit.UIApplication.SharedApplication.KeyWindow?.EndEditing(true);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.IsNavigatingAway)
            await _viewModel.InitializeAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (_viewModel.IsNavigatingAway)
            return;

        await _viewModel.CleanupAsync();
    }
}