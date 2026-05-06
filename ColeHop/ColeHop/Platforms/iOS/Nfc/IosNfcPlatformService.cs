using ColeHop.Services.Nfc;

namespace ColeHop.Platforms.iOS.Nfc
{
    public sealed class IosNfcPlatformService : INfcPlatformService
    {
        public bool IsSupported => false; // TODO: Implementar NFC para iOS

        public bool IsEnabled => false;

        public Task StartListeningAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopListeningAsync()
        {
            return Task.CompletedTask;
        }

        public Task WaitForTagAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException("NFC no está implementado para iOS aún.");
        }

        public Task<NfcScanResult> TransceiveAsync(byte[] apdu, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("NFC no está implementado para iOS aún.");
        }
    }
}
