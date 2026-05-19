using ColeHop.Services.Nfc;

namespace ColeHop.Services.Nfc
{
    public interface INfcService
    {
        bool IsSupported { get; }
        bool IsEnabled { get; }

        Task StartAsync();
        Task StopAsync();
        Task BeginDnieReadingAsync(string can, CancellationToken cancellationToken, IProgress<string>? progress = null);

        event EventHandler<VerifiedIdentity> IdentityVerified;
    }
}