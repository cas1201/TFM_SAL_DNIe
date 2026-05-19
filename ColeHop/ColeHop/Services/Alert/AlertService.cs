namespace ColeHop.Services.Alert
{
    public sealed class AlertService : IAlertService
    {
        public async Task ShowAsync(string title, string message, string cancel = "OK")
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;

            var popup = new Views.CustomAlertPopup(title, message, cancel, null);
            await page.Navigation.PushModalAsync(popup, animated: false);
            await popup.WaitForDismissAsync();
        }

        public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancelar")
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return false;

            var popup = new Views.CustomAlertPopup(title, message, cancel, accept);
            await page.Navigation.PushModalAsync(popup, animated: false);
            return await popup.WaitForConfirmAsync();
        }
    }
}
