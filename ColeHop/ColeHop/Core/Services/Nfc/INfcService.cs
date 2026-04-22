using ColeHop.Core.Services.Nfc.Dtos;

namespace ColeHop.Core.Services.Nfc
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