using ColeHop.Core.Services.Nfc;
using ColeHop.Core.Services.Nfc.Dtos;
using ColeHop.Services.Nfc.Dnie;

namespace ColeHop.Services.Nfc
{
    public sealed class NfcService : INfcService
    {
        private readonly INfcPlatformService _platformService;

        public bool IsSupported => _platformService.IsSupported;
        public bool IsEnabled => _platformService.IsEnabled;

        public event EventHandler<VerifiedIdentity>? IdentityVerified;

        public NfcService(INfcPlatformService platformService)
        {
            _platformService = platformService;
        }

        public async Task StartAsync()
        {
            if (!IsSupported)
                throw new InvalidOperationException("El dispositivo no soporta NFC.");

            if (!IsEnabled)
                throw new InvalidOperationException("NFC no está activado.");

            await _platformService.StartListeningAsync();
        }

        public async Task StopAsync()
        {
            await _platformService.StopListeningAsync();
        }

        public async Task BeginDnieReadingAsync(string can, CancellationToken cancellationToken)
        {
            await StartAsync();

            try
            {
                var session = new DnieSession(_platformService, can);
                var identity = await session.ExecuteAsync(cancellationToken);
                IdentityVerified?.Invoke(this, identity);
            }
            finally
            {
                await StopAsync();
            }
        }
    }
}