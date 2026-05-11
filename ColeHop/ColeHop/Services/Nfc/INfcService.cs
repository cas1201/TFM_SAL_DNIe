using ColeHop.Services.Nfc;

namespace ColeHop.Services.Nfc
{
    public interface INfcService
    {
        bool IsSupported { get; }
        bool IsEnabled { get; }

        Task StartAsync();
        Task StopAsync();
        Task BeginDnieReadingAsync(string can, CancellationToken cancellationToken);

        event EventHandler<VerifiedIdentity> IdentityVerified;
    }
}