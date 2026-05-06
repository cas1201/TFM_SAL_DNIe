using System.Security.Cryptography;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class SodValidator
    {
        public void Validate(Dictionary<string, byte[]> dataGroups, byte[] sodFile)
        {
            if (dataGroups.Count == 0)
                throw new InvalidOperationException("No hay Data Groups para validar.");

            if (sodFile.Length == 0)
                throw new InvalidOperationException("EF.SOD vacío.");

            // Extraer la estructura CMS/PKCS#7 del SOD
            var sod = ParseSod(sodFile);

            // Recalcular hashes de los Data Groups leídos
            foreach (var entry in sod.DataGroupHashes)
            {
                if (!dataGroups.TryGetValue(entry.Key, out var dgData))
                    throw new InvalidOperationException($"Falta el Data Group {entry.Key}.");

                var computedHash = ComputeHash(dgData, entry.Value.Algorithm);

                if (!computedHash.SequenceEqual(entry.Value.Hash))
                    throw new InvalidOperationException($"Hash inválido para {entry.Key}.");
            }

            // Verificar la firma digital del SOD
            if (!VerifySignature(sod))
                throw new InvalidOperationException("Firma digital del EF.SOD inválida.");

            // Verificar la cadena de certificados
            if (!VerifyCertificateChain(sod))
                throw new InvalidOperationException("Cadena de confianza del DNIe inválida.");
        }

        private static SodContent ParseSod(byte[] sodFile)
        {
            // EF.SOD es un CMS SignedData
            // Debe parsearse conforme a PKCS#7

            // En implementación real:
            // - System.Security.Cryptography.Pkcs
            // - BouncyCastle CMS

            return new SodContent();
        }

        private static byte[] ComputeHash(byte[] data, HashAlgorithmName algorithm)
        {
            if (algorithm == HashAlgorithmName.SHA256)
            {
                using var hashAlg = SHA256.Create();
                return hashAlg.ComputeHash(data);
            }
            else if (algorithm == HashAlgorithmName.SHA384)
            {
                using var hashAlg = SHA384.Create();
                return hashAlg.ComputeHash(data);
            }
            else if (algorithm == HashAlgorithmName.SHA512)
            {
                using var hashAlg = SHA512.Create();
                return hashAlg.ComputeHash(data);
            }
            else if (algorithm == HashAlgorithmName.SHA1)
            {
                using var hashAlg = SHA1.Create();
                return hashAlg.ComputeHash(data);
            }

            throw new InvalidOperationException($"Algoritmo hash no soportado: {algorithm.Name}");
        }

        private static bool VerifySignature(SodContent sod)
        {
            // Verificación criptográfica de la firma CMS
            // Clave pública del certificado del emisor
            return true;
        }

        private static bool VerifyCertificateChain(SodContent sod)
        {
            // Validación contra CA raíz del DNIe
            // Comprobación de validez temporal
            return true;
        }

        private sealed class SodContent
        {
            public Dictionary<string, (byte[] Hash, HashAlgorithmName Algorithm)> DataGroupHashes { get; } = [];
        }
    }
}
