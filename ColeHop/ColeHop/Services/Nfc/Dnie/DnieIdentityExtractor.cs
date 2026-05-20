using ColeHop.Helpers;
using ColeHop.Resources.Strings;

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
            // DNIe español usa formato TD1 (ICAO 9303): 3 líneas de 30 caracteres
            // Línea 1: Tipo(2) + País(3) + NumDocumento(9) + Check(1) + DatosOpcionales(15)
            // Línea 2: FechaNac(6) + Check(1) + Sexo(1) + FechaExp(6) + Check(1) + Nacionalidad(3) + Opcionales(11) + CheckGlobal(1)
            // Línea 3: APELLIDOS<<NOMBRE<<<...
            const int lineLength = 30;

            var cleanMrz = mrz.Replace("\n", "").Replace("\r", "");

            if (cleanMrz.Length < lineLength * 2)
                throw new InvalidOperationException($"MRZ demasiado corta: {cleanMrz.Length} chars (minimo 60).");

            var line1 = cleanMrz[..lineLength];
            var line2 = cleanMrz[lineLength..(lineLength * 2)];
            var line3 = cleanMrz.Length >= lineLength * 3
                ? cleanMrz[(lineLength * 2)..(lineLength * 3)]
                : string.Empty;

            // Línea 1: número de soporte en posiciones [5..14], DNI real en datos opcionales [15..24]
            var supportNumber = line1[5..14].TrimEnd('<');
            var optionalData1 = line1[15..].TrimEnd('<');
            // En el DNIe español, el número de DNI (8 dígitos + letra) está en los datos opcionales de la línea 1
            var documentNumber = ExtractDniFromOptionalData(optionalData1, supportNumber);

            // Línea 2: fecha nacimiento [0..6], sexo [7], expiración [8..14], nacionalidad [15..18]
            var dateOfBirth = ParseDate(line2[..6]);
            var sex = line2[7] == 'M' ? AppResources.SexMasculine : line2[7] == 'F' ? AppResources.SexFemenine : AppResources.SexNotDefined;
            var expirationDate = ParseDate(line2[8..14]);
            var nationality = line2[15..18].TrimEnd('<');

            // Línea 3: APELLIDOS<<NOMBRE
            var (surnames, givenNames) = ParseNamesFromLine3(line3);

            System.Diagnostics.Debug.WriteLine($"[Identity] MRZ Line1: {line1}");
            System.Diagnostics.Debug.WriteLine($"[Identity] MRZ Line2: {line2}");
            System.Diagnostics.Debug.WriteLine($"[Identity] MRZ Line3: {line3}");
            System.Diagnostics.Debug.WriteLine($"[Identity] Soporte: {supportNumber}, DNI: {documentNumber}");

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

        private static (string surnames, string givenNames) ParseNamesFromLine3(string line3)
        {
            if (string.IsNullOrEmpty(line3))
                return (string.Empty, string.Empty);

            var parts = line3.Split("<<", StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                var single = parts[0].Replace('<', ' ').Trim();
                return (single, string.Empty);
            }

            var surnames = parts[0].Replace('<', ' ').Trim();
            var givenNames = parts[1].Replace('<', ' ').Trim();

            return (surnames, givenNames);
        }

        private static string ExtractDniFromOptionalData(string optionalData, string supportNumber)
        {
            // En el DNIe español, los datos opcionales de la línea 1 contienen el número de DNI
            // Formato: 8 dígitos + 1 letra (ej: "12345678Z")
            // Buscar patrón de DNI español en los datos opcionales
            if (!string.IsNullOrEmpty(optionalData))
            {
                // Eliminar dígitos de control al final y buscar patrón DNI (8 dígitos + letra)
                var clean = optionalData.TrimEnd('<');
                for (int i = 0; i <= clean.Length - 9; i++)
                {
                    var candidate = clean[i..(i + 9)];
                    if (candidate.Length == 9 &&
                        char.IsDigit(candidate[0]) && char.IsDigit(candidate[1]) &&
                        char.IsDigit(candidate[2]) && char.IsDigit(candidate[3]) &&
                        char.IsDigit(candidate[4]) && char.IsDigit(candidate[5]) &&
                        char.IsDigit(candidate[6]) && char.IsDigit(candidate[7]) &&
                        char.IsLetter(candidate[8]))
                    {
                        return candidate;
                    }
                }
            }

            // Fallback: devolver número de soporte si no se encuentra el DNI
            return supportNumber;
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
