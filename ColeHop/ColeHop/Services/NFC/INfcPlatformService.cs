namespace ColeHop.Services.Nfc
{
    public interface INfcPlatformService
    {
        bool IsSupported { get; }
        bool IsEnabled { get; }

        Task StartListeningAsync();
        Task StopListeningAsync();
        Task<NfcScanResult> TransceiveAsync(byte[] apdu, CancellationToken cancellationToken);
    }
}