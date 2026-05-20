namespace ColeHop.Services.Alert
{
    public interface IAlertService
    {
        Task ShowAsync(string title, string message, string? cancel = null, AlertIcon icon = AlertIcon.Info);
        Task<bool> ShowConfirmAsync(string title, string message, string? accept = null, string? cancel = null, AlertIcon icon = AlertIcon.Info);
    }
}
