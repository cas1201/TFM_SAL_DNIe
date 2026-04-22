namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieFileReader
    {
        private readonly INfcPlatformService _platformService;

        public DnieFileReader(INfcPlatformService platformService)
        {
            _platformService = platformService;
        }

        public Task<Dictionary<string, byte[]>> ReadDataGroupsAsync(CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, byte[]>();

            // DG1, DG2, etc. se leerían mediante APDUs SELECT/READ
            result["DG1"] = Array.Empty<byte>();
            result["DG2"] = Array.Empty<byte>();

            return Task.FromResult(result);
        }

        public Task<byte[]> ReadSodAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
