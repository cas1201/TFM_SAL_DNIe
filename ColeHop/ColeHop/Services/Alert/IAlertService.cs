namespace ColeHop.Services.Alert
{
    public interface IAlertService
    {
        Task ShowAsync(string title, string message, string cancel = "OK");
        Task<bool> ShowConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancelar");
    }
}
