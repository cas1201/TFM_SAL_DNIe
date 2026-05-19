using ColeHop.Helpers;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieFileReader
    {
        private readonly INfcPlatformService _platformService;
        private readonly SecureMessagingContext _smContext;

        private static readonly byte[] MrtdAid = [0xA0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01];

        private const ushort FidDg1 = 0x0101;
        private const ushort FidDg2 = 0x0102;
        private const ushort FidSod = 0x011D;
        private const int MaxChunkSize = 0xDF;

        public DnieFileReader(INfcPlatformService platformService, SecureMessagingContext smContext)
        {
            _platformService = platformService;
            _smContext = smContext;
        }

        public async Task<Dictionary<string, byte[]>> ReadDataGroupsAsync(CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, byte[]>();

            await SelectMrtdApplicationAsync(cancellationToken);

            try
            {
                var dg1 = await ReadFileAsync(FidDg1, cancellationToken);
                result["DG1"] = dg1;
                System.Diagnostics.Debug.WriteLine($"[DG] DG1 leido: {dg1.Length} bytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DG] Error leyendo DG1: {ex.Message}");
            }

            try
            {
                var dg2 = await ReadFileAsync(FidDg2, cancellationToken);
                result["DG2"] = dg2;
                System.Diagnostics.Debug.WriteLine($"[DG] DG2 leido: {dg2.Length} bytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DG] Error leyendo DG2: {ex.Message}");
            }

            return result;
        }

        public async Task<byte[]> ReadSodAsync(CancellationToken cancellationToken)
        {
            await SelectMrtdApplicationAsync(cancellationToken);
            var sod = await ReadFileAsync(FidSod, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[DG] SOD leido: {sod.Length} bytes");
            return sod;
        }

        private async Task SelectMrtdApplicationAsync(CancellationToken cancellationToken)
        {
            var apdu = new List<byte> { 0x00, 0xA4, 0x04, 0x0C, (byte)MrtdAid.Length };
            apdu.AddRange(MrtdAid);

            await SendProtectedApduAsync(apdu.ToArray(), cancellationToken);
            System.Diagnostics.Debug.WriteLine("[DG] Aplicacion eMRTD seleccionada");
        }

        private async Task SelectFileAsync(ushort fid, CancellationToken cancellationToken)
        {
            var apdu = new byte[]
            {
                0x00, 0xA4, 0x02, 0x0C,
                0x02,
                (byte)(fid >> 8),
                (byte)(fid & 0xFF)
            };

            await SendProtectedApduAsync(apdu, cancellationToken);
        }

        private async Task<byte[]> ReadFileAsync(ushort fid, CancellationToken cancellationToken)
        {
            await SelectFileAsync(fid, cancellationToken);

            // Leer cabecera para determinar tamaño total
            var header = await ReadBinaryAsync(0, 4, cancellationToken);

            int totalLength = ParseFileLength(header);
            System.Diagnostics.Debug.WriteLine($"[DG] Archivo {fid:X4}: {totalLength} bytes totales");

            // Leer el archivo completo por chunks
            var data = new List<byte>();
            int offset = 0;

            while (offset < totalLength)
            {
                int toRead = Math.Min(MaxChunkSize, totalLength - offset);
                var chunk = await ReadBinaryAsync(offset, toRead, cancellationToken);
                data.AddRange(chunk);
                offset += chunk.Length;

                if (chunk.Length < toRead)
                    break;
            }

            return data.ToArray();
        }

        private async Task<byte[]> ReadBinaryAsync(int offset, int length, CancellationToken cancellationToken)
        {
            var apdu = new byte[]
            {
                0x00, 0xB0,
                (byte)(offset >> 8),
                (byte)(offset & 0xFF),
                (byte)length
            };

            return await SendProtectedApduAsync(apdu, cancellationToken);
        }

        private async Task<byte[]> SendProtectedApduAsync(byte[] plainApdu, CancellationToken cancellationToken)
        {
            var protectedApdu = _smContext.ProtectApdu(plainApdu);

            var result = await _platformService.TransceiveAsync(protectedApdu, cancellationToken);

            if (!result.IsValid || result.RawData == null)
                throw new InvalidOperationException(result.ErrorMessage ?? "Error NFC durante lectura.");

            var raw = result.RawData;

            if (raw.Length < 2)
                throw new InvalidOperationException("Respuesta NFC demasiado corta.");

            var sw = (ushort)((raw[^2] << 8) | raw[^1]);
            if (sw != 0x9000)
                throw new InvalidOperationException($"Error lectura SW={sw:X4}");

            if (raw.Length == 2)
                return [];

            var plainResponse = _smContext.UnprotectResponse(raw);

            // Quitar SW del resultado
            if (plainResponse.Length >= 2)
            {
                var data = new byte[plainResponse.Length - 2];
                Array.Copy(plainResponse, 0, data, 0, data.Length);
                return data;
            }

            return [];
        }

        private static int ParseFileLength(byte[] header)
        {
            if (header.Length < 2)
                throw new ArgumentException("Cabecera de archivo demasiado corta.");

            int offset = 1; // Saltar primer tag byte

            // Tag de 2 bytes (ej: 7F61)
            if ((header[0] & 0x1F) == 0x1F)
                offset = 2;

            int length = Asn1Utils.ParseTlvLength(header, offset, out int lenBytes);
            return offset + lenBytes + length;
        }
    }
}
