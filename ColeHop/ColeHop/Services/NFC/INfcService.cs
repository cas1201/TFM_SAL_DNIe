namespace ColeHop.Services.NFC
{
    public interface INfcService
    {
        bool IsSupported { get; }
        bool IsEnabled { get; }

        Task StartAsync();
        Task StopAsync();

        event EventHandler<NfcScanResult> TagDetected;
    }
}
