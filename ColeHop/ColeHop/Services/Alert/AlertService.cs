using ColeHop.Resources.Strings;

namespace ColeHop.Services.Alert
{
    public sealed class AlertService : IAlertService
    {
        public async Task ShowAsync(string title, string message, string? cancel = null, AlertIcon icon = AlertIcon.Info)
        {
            if (cancel  == null) cancel = AppResources.OK;

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;

            var popup = new Views.CustomAlertPopup(title, message, cancel, null, icon);
            await page.Navigation.PushModalAsync(popup, animated: false);
            await popup.WaitForDismissAsync();
        }

        public async Task<bool> ShowConfirmAsync(string title, string message, string? accept = null, string? cancel = null, AlertIcon icon = AlertIcon.Info)
        {
            if (accept == null) accept = AppResources.OK; 
            if (cancel == null) cancel = AppResources.Cancel;

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return false;

            var popup = new Views.CustomAlertPopup(title, message, cancel, accept, icon);
            await page.Navigation.PushModalAsync(popup, animated: false);
            return await popup.WaitForConfirmAsync();
        }
    }
}
