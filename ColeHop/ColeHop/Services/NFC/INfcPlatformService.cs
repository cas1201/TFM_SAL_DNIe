namespace ColeHop.Services.Nfc
{
    public interface INfcPlatformService
    {
        bool IsSupported { get; }
        bool IsEnabled { get; }

        Task StartListeningAsync();
        Task StopListeningAsync();
        Task WaitForTagAsync(CancellationToken cancellationToken);
        Task<NfcScanResult> TransceiveAsync(byte[] apdu, CancellationToken cancellationToken);
    }
}