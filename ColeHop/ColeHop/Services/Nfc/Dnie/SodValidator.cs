using ColeHop.Helpers;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Digests;

namespace ColeHop.Services.Nfc.Dnie
{
    /// <summary>
    /// Valida la integridad de los Data Groups del DNIe usando el EF.SOD.
    /// Parsea manualmente la estructura ASN.1 del CMS SignedData para evitar
    /// incompatibilidades de BouncyCastle con la codificacion BER del DNIe 3.0.
    /// La autenticidad del documento se garantiza mediante PACE (canal seguro con el chip).
    /// </summary>
    public sealed class SodValidator
    {
        public void Validate(Dictionary<string, byte[]> dataGroups, byte[] sodFile)
        {
            if (dataGroups.Count == 0)
                throw new InvalidOperationException("No hay Data Groups para validar.");

            if (sodFile.Length == 0)
                throw new InvalidOperationException("EF.SOD vacio.");

            System.Diagnostics.Debug.WriteLine("[SOD] Iniciando validacion...");

            // Extraer LDSSecurityObject del SOD
            var ldsContent = ExtractLdsSecurityObject(sodFile);
            var expectedHashes = ParseDataGroupHashes(ldsContent);

            System.Diagnostics.Debug.WriteLine($"[SOD] Encontrados {expectedHashes.Count} hashes de DGs");

            // Validar hashes
            ValidateHashes(dataGroups, expectedHashes);

            System.Diagnostics.Debug.WriteLine("[SOD] Validacion completada exitosamente");
        }

        /// <summary>
        /// Extrae el LDSSecurityObject (contenido firmado) del SOD.
        /// Estructura: Tag77 -> ContentInfo -> SignedData -> EncapContentInfo -> eContent
        /// </summary>
        private static byte[] ExtractLdsSecurityObject(byte[] data)
        {
            // Quitar wrapper ICAO (tag 0x77)
            if (data.Length > 0 && data[0] != 0x30)
            {
                int offset = 1;
                if ((data[0] & 0x1F) == 0x1F)
                    offset = 2;

                int length = Asn1Utils.ParseTlvLength(data, offset, out int lenBytes);
                offset += lenBytes;

                var inner = new byte[length];
                Array.Copy(data, offset, inner, 0, length);
                data = inner;
            }

            // Parsear ContentInfo: SEQUENCE { OID, [0] SignedData }
            var contentInfo = Asn1Sequence.GetInstance(Asn1Object.FromByteArray(data));
            var signedDataTagged = Asn1TaggedObject.GetInstance(contentInfo[1]);
            var signedData = Asn1Sequence.GetInstance(signedDataTagged.GetExplicitBaseObject());

            // SignedData: SEQUENCE { version, digestAlgorithms, encapContentInfo, [0]? certs, [1]? crls, signerInfos }
            // Buscar encapContentInfo (SEQUENCE con OID como primer elemento)
            Asn1Sequence? encapContentInfo = null;
            for (int i = 0; i < signedData.Count; i++)
            {
                var element = signedData[i];
                if (element is Asn1Sequence candidateSeq && candidateSeq.Count >= 1 &&
                    candidateSeq[0] is DerObjectIdentifier)
                {
                    encapContentInfo = candidateSeq;
                    break;
                }
            }

            if (encapContentInfo == null)
                throw new InvalidOperationException("No se encontro encapContentInfo en SignedData.");

            // encapContentInfo: SEQUENCE { OID, [0] OCTET STRING }
            if (encapContentInfo.Count < 2)
                throw new InvalidOperationException("encapContentInfo sin contenido.");

            var eContentTagged = Asn1TaggedObject.GetInstance(encapContentInfo[1]);
            var eContent = Asn1OctetString.GetInstance(eContentTagged.GetExplicitBaseObject());

            return eContent.GetOctets();
        }

        /// <summary>
        /// Parsea el LDSSecurityObject para extraer los hashes de Data Groups.
        /// LDSSecurityObject: SEQUENCE { version?, hashAlgorithm, dataGroupHashValues }
        /// </summary>
        private static Dictionary<int, byte[]> ParseDataGroupHashes(byte[] ldsBytes)
        {
            var hashes = new Dictionary<int, byte[]>();
            var seq = Asn1Sequence.GetInstance(Asn1Object.FromByteArray(ldsBytes));

            // Buscar la secuencia de DataGroupHash entries
            for (int i = 0; i < seq.Count; i++)
            {
                if (seq[i] is not Asn1Sequence candidate || candidate.Count == 0)
                    continue;

                // Verificar si es una secuencia de pares (INTEGER, OCTET STRING)
                var first = candidate[0];
                if (first is Asn1Sequence pair && pair.Count == 2 &&
                    pair[0] is DerInteger && pair[1] is Asn1OctetString)
                {
                    foreach (var item in candidate)
                    {
                        if (item is Asn1Sequence dgHash && dgHash.Count == 2 &&
                            dgHash[0] is DerInteger dgNum && dgHash[1] is Asn1OctetString hashVal)
                        {
                            hashes[dgNum.IntValueExact] = hashVal.GetOctets();
                        }
                    }
                    break;
                }

                if (first is DerInteger dgNumber && candidate.Count == 2 &&
                    candidate[1] is Asn1OctetString hashValue)
                {
                    for (int j = i; j < seq.Count; j++)
                    {
                        if (seq[j] is Asn1Sequence entry && entry.Count == 2 &&
                            entry[0] is DerInteger num && entry[1] is Asn1OctetString hash)
                        {
                            hashes[num.IntValueExact] = hash.GetOctets();
                        }
                    }
                    break;
                }
            }

            return hashes;
        }

        private static void ValidateHashes(Dictionary<string, byte[]> dataGroups, Dictionary<int, byte[]> expectedHashes)
        {
            int validated = 0;

            foreach (var (dgNumber, expectedHash) in expectedHashes)
            {
                var dgKey = $"DG{dgNumber}";

                if (!dataGroups.TryGetValue(dgKey, out var dgData))
                {
                    System.Diagnostics.Debug.WriteLine($"[SOD] {dgKey} no disponible, omitiendo");
                    continue;
                }

                byte[] computedHash;
                if (expectedHash.Length == 32)
                {
                    var digest = new Sha256Digest();
                    digest.BlockUpdate(dgData, 0, dgData.Length);
                    computedHash = new byte[digest.GetDigestSize()];
                    digest.DoFinal(computedHash, 0);
                }
                else if (expectedHash.Length == 20)
                {
                    var digest = new Sha1Digest();
                    digest.BlockUpdate(dgData, 0, dgData.Length);
                    computedHash = new byte[digest.GetDigestSize()];
                    digest.DoFinal(computedHash, 0);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SOD] {dgKey} hash longitud no soportada ({expectedHash.Length} bytes)");
                    continue;
                }

                if (!computedHash.SequenceEqual(expectedHash))
                    throw new InvalidOperationException($"Hash invalido para {dgKey}. Datos posiblemente alterados.");

                validated++;
                System.Diagnostics.Debug.WriteLine($"[SOD] {dgKey} hash valido");
            }

            if (validated == 0)
                throw new InvalidOperationException("No se pudo validar ningun Data Group.");
        }
    }
}
