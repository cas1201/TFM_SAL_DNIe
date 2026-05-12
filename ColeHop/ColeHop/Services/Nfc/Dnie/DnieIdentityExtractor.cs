using ColeHop.Helpers;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieIdentityExtractor
    {
        public VerifiedIdentity Extract(Dictionary<string, byte[]> dataGroups)
        {
            if (!dataGroups.TryGetValue("DG1", out var dg1Data) || dg1Data.Length == 0)
                throw new InvalidOperationException("DG1 no disponible para extraer identidad.");

            var mrzBytes = ExtractMrzFromDg1(dg1Data);
            var mrz = System.Text.Encoding.ASCII.GetString(mrzBytes);

            System.Diagnostics.Debug.WriteLine($"[Identity] MRZ: {mrz}");

            var identity = ParseTd1Mrz(mrz);

            // Intentar extraer foto de DG2
            byte[]? faceImage = null;
            if (dataGroups.TryGetValue("DG2", out var dg2Data) && dg2Data.Length > 0)
                faceImage = ExtractFaceImageFromDg2(dg2Data);

            return new VerifiedIdentity
            {
                DocumentNumber = identity.DocumentNumber,
                GivenNames = identity.GivenNames,
                Surnames = identity.Surnames,
                DateOfBirth = identity.DateOfBirth,
                Sex = identity.Sex,
                ExpirationDate = identity.ExpirationDate,
                Nationality = identity.Nationality,
                FaceImage = faceImage
            };
        }

        private static byte[] ExtractMrzFromDg1(byte[] dg1Data)
        {
            // DG1: tag 61, dentro tag 5F1F contiene la MRZ
            int offset = 0;

            // Buscar tag 5F1F
            while (offset < dg1Data.Length - 3)
            {
                if (dg1Data[offset] == 0x5F && dg1Data[offset + 1] == 0x1F)
                {
                    offset += 2;
                    int length = Asn1Utils.ParseTlvLength(dg1Data, offset, out int lenBytes);
                    offset += lenBytes;

                    var mrz = new byte[length];
                    Array.Copy(dg1Data, offset, mrz, 0, length);
                    return mrz;
                }

                offset++;
            }

            // Fallback: saltar tag y length del contenedor 61
            offset = 0;
            if (dg1Data[0] == 0x61)
            {
                offset = 1;
                Asn1Utils.ParseTlvLength(dg1Data, offset, out int lenBytes);
                offset += lenBytes;

                if (dg1Data[offset] == 0x5F && dg1Data[offset + 1] == 0x1F)
                {
                    offset += 2;
                    int length = Asn1Utils.ParseTlvLength(dg1Data, offset, out int lb);
                    offset += lb;

                    var mrz = new byte[length];
                    Array.Copy(dg1Data, offset, mrz, 0, length);
                    return mrz;
                }
            }

            throw new InvalidOperationException("No se encontro MRZ (tag 5F1F) en DG1.");
        }

        private static ParsedMrz ParseTd1Mrz(string mrz)
        {
            // DNIe espanol usa formato TD1: 3 lineas de 30 caracteres
            const int lineLength = 30;

            var cleanMrz = mrz.Replace("\n", "").Replace("\r", "");

            if (cleanMrz.Length < lineLength * 2)
                throw new InvalidOperationException($"MRZ demasiado corta: {cleanMrz.Length} chars (minimo 60).");

            var line1 = cleanMrz[..lineLength];
            var line2 = cleanMrz[lineLength..(lineLength * 2)];
            var line3 = cleanMrz.Length >= lineLength * 3
                ? cleanMrz[(lineLength * 2)..(lineLength * 3)]
                : string.Empty;

            // Linea 1: IDESP + apellidos << nombres
            var namesSection = line1[5..];
            var (surnames, givenNames) = ParseNames(namesSection, line3);

            // Linea 2: documento(9) + check(1) + nacionalidad(3) + nacimiento(6) + check(1) + sexo(1) + expiracion(6) + check(1) + ...
            var documentNumber = line2[..9].TrimEnd('<');
            var nationality = line2[10..13];
            var dateOfBirth = ParseDate(line2[13..19]);
            var sex = line2[20] == 'M' ? "Masculino" : line2[20] == 'F' ? "Femenino" : "No especificado";
            var expirationDate = ParseDate(line2[21..27]);

            System.Diagnostics.Debug.WriteLine($"[Identity] Extraido: {givenNames} {surnames} - {documentNumber}");

            return new ParsedMrz
            {
                DocumentNumber = documentNumber,
                GivenNames = givenNames,
                Surnames = surnames,
                DateOfBirth = dateOfBirth,
                Sex = sex,
                ExpirationDate = expirationDate,
                Nationality = nationality
            };
        }

        private static (string surnames, string givenNames) ParseNames(string namesSection, string line3)
        {
            var fullNames = namesSection;
            if (!string.IsNullOrEmpty(line3))
                fullNames += line3;

            var parts = fullNames.Split("<<", StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                var single = parts[0].Replace('<', ' ').Trim();
                return (single, string.Empty);
            }

            var surnames = parts[0].Replace('<', ' ').Trim();
            var givenNames = parts[1].Replace('<', ' ').Trim();

            return (surnames, givenNames);
        }

        private static DateTime ParseDate(string yymmdd)
        {
            if (yymmdd.Length < 6 || yymmdd.Contains('<'))
                return DateTime.MinValue;

            if (!int.TryParse(yymmdd[..2], out int year) ||
                !int.TryParse(yymmdd[2..4], out int month) ||
                !int.TryParse(yymmdd[4..6], out int day))
                return DateTime.MinValue;

            year += year < 50 ? 2000 : 1900;

            if (month < 1 || month > 12 || day < 1 || day > 31)
                return DateTime.MinValue;

            return new DateTime(year, month, day);
        }

        private static byte[]? ExtractFaceImageFromDg2(byte[] dg2Data)
        {
            // DG2 contiene imagen facial en formato JPEG/JPEG2000
            // Buscar marcador JPEG (FF D8) o JPEG2000 (00 00 00 0C 6A 50)
            for (int i = 0; i < dg2Data.Length - 1; i++)
            {
                if (dg2Data[i] == 0xFF && dg2Data[i + 1] == 0xD8)
                {
                    var image = new byte[dg2Data.Length - i];
                    Array.Copy(dg2Data, i, image, 0, image.Length);
                    return image;
                }

                if (i + 5 < dg2Data.Length &&
                    dg2Data[i] == 0x00 && dg2Data[i + 1] == 0x00 &&
                    dg2Data[i + 2] == 0x00 && dg2Data[i + 3] == 0x0C &&
                    dg2Data[i + 4] == 0x6A && dg2Data[i + 5] == 0x50)
                {
                    var image = new byte[dg2Data.Length - i];
                    Array.Copy(dg2Data, i, image, 0, image.Length);
                    return image;
                }
            }

            return null;
        }

        private sealed class ParsedMrz
        {
            public string DocumentNumber { get; init; } = string.Empty;
            public string GivenNames { get; init; } = string.Empty;
            public string Surnames { get; init; } = string.Empty;
            public DateTime DateOfBirth { get; init; }
            public string Sex { get; init; } = string.Empty;
            public DateTime ExpirationDate { get; init; }
            public string Nationality { get; init; } = string.Empty;
        }
    }
}
