using ColeHop.Core.Services.Nfc.Dtos;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieSession
    {
        private readonly INfcPlatformService _platformService;
        private readonly string _can;

        public DnieSession(INfcPlatformService platformService, string can)
        {
            _platformService = platformService;
            _can = can;
        }

        public async Task<VerifiedIdentity> ExecuteAsync(CancellationToken cancellationToken)
        {
            var paceSession = new PaceSession(_platformService);
            await paceSession.EstablishSecureChannelAsync(_can, cancellationToken);

            var fileReader = new DnieFileReader(_platformService);
            var dgs = await fileReader.ReadDataGroupsAsync(cancellationToken);
            var sod = await fileReader.ReadSodAsync(cancellationToken);

            var sodValidator = new SodValidator();
            sodValidator.Validate(dgs, sod);

            var extractor = new DnieIdentityExtractor();
            return extractor.Extract(dgs);
        }
    }
}
