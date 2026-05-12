using ColeHop.Helpers;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class SodValidator
    {
        public void Validate(Dictionary<string, byte[]> dataGroups, byte[] sodFile)
        {
            if (dataGroups.Count == 0)
                throw new InvalidOperationException("No hay Data Groups para validar.");

            if (sodFile.Length == 0)
                throw new InvalidOperationException("EF.SOD vacio.");

            System.Diagnostics.Debug.WriteLine("[SOD] Iniciando validacion...");

            var signedData = new CmsSignedData(sodFile);

            var ldsHashes = ExtractDataGroupHashes(signedData);
            ValidateHashes(dataGroups, ldsHashes);

            VerifySignature(signedData);

            System.Diagnostics.Debug.WriteLine("[SOD] Validacion completada exitosamente");
        }

        private static Dictionary<int, byte[]> ExtractDataGroupHashes(CmsSignedData signedData)
        {
            var contentStream = signedData.SignedContent;
            using var ms = new System.IO.MemoryStream();
            contentStream.Write(ms);
            var contentBytes = ms.ToArray();
            var content = Asn1OctetString.GetInstance(contentBytes);
            var seq = Asn1Sequence.GetInstance(content.GetOctets());

            // LDSSecurityObject: version, hashAlgorithm, dataGroupHashValues
            var hashes = new Dictionary<int, byte[]>();

            // dataGroupHashValues es la tercera posicion (indice 2) si hay version,
            // o segunda posicion (indice 1) si no hay version explicita
            Asn1Sequence? dgHashSeq = null;

            for (int i = 0; i < seq.Count; i++)
            {
                var element = seq[i];
                if (element is Asn1Sequence innerSeq && innerSeq.Count > 0)
                {
                    var first = innerSeq[0];
                    if (first is Asn1Sequence dgEntry && dgEntry.Count == 2)
                    {
                        dgHashSeq = innerSeq;
                        break;
                    }

                    if (first is DerInteger)
                    {
                        dgHashSeq = innerSeq;
                        break;
                    }
                }
            }

            if (dgHashSeq == null)
            {
                // Buscar directamente secuencias con (INTEGER, OCTET STRING)
                for (int i = 0; i < seq.Count; i++)
                {
                    if (seq[i] is Asn1Sequence candidate)
                    {
                        foreach (var item in candidate)
                        {
                            if (item is Asn1Sequence pair && pair.Count == 2 &&
                                pair[0] is DerInteger dgNum && pair[1] is Asn1OctetString hashVal)
                            {
                                hashes[dgNum.IntValueExact] = hashVal.GetOctets();
                            }
                        }

                        if (hashes.Count > 0)
                            break;
                    }
                }
            }
            else
            {
                foreach (var item in dgHashSeq)
                {
                    if (item is Asn1Sequence pair && pair.Count == 2 &&
                        pair[0] is DerInteger dgNum && pair[1] is Asn1OctetString hashVal)
                    {
                        hashes[dgNum.IntValueExact] = hashVal.GetOctets();
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[SOD] Encontrados {hashes.Count} hashes de DGs");
            return hashes;
        }

        private static void ValidateHashes(Dictionary<string, byte[]> dataGroups, Dictionary<int, byte[]> expectedHashes)
        {
            foreach (var (dgNumber, expectedHash) in expectedHashes)
            {
                var dgKey = $"DG{dgNumber}";

                if (!dataGroups.TryGetValue(dgKey, out var dgData))
                {
                    System.Diagnostics.Debug.WriteLine($"[SOD] {dgKey} no disponible, omitiendo");
                    continue;
                }

                var digest = new Sha256Digest();
                digest.BlockUpdate(dgData, 0, dgData.Length);
                var computedHash = new byte[digest.GetDigestSize()];
                digest.DoFinal(computedHash, 0);

                if (!computedHash.SequenceEqual(expectedHash))
                    throw new InvalidOperationException($"Hash invalido para {dgKey}. Datos posiblemente alterados.");

                System.Diagnostics.Debug.WriteLine($"[SOD] {dgKey} hash valido");
            }
        }

        private static void VerifySignature(CmsSignedData signedData)
        {
            var signers = signedData.GetSignerInfos();
            var signerEnum = signers.GetSigners().GetEnumerator();

            if (!signerEnum.MoveNext())
                throw new InvalidOperationException("No se encontro firmante en el SOD.");

            var signer = (SignerInformation)signerEnum.Current;

            var certStore = signedData.GetCertificates();
            var certs = new List<X509Certificate>();

            foreach (X509Certificate cert in certStore.EnumerateMatches(null))
                certs.Add(cert);

            if (certs.Count == 0)
                throw new InvalidOperationException("No se encontraron certificados en el SOD.");

            X509Certificate? signerCert = null;
            var sid = signer.SignerID;

            foreach (var cert in certs)
            {
                if (sid.Match(cert))
                {
                    signerCert = cert;
                    break;
                }
            }

            signerCert ??= certs[0];

            if (!signer.Verify(signerCert))
                throw new InvalidOperationException("Firma digital del SOD invalida.");

            System.Diagnostics.Debug.WriteLine("[SOD] Firma verificada correctamente");

            // Verificar vigencia del certificado
            try
            {
                signerCert.CheckValidity(DateTime.UtcNow);
                System.Diagnostics.Debug.WriteLine($"[SOD] Certificado vigente. Emisor: {signerCert.IssuerDN}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SOD] Advertencia certificado: {ex.Message}");
            }
        }
    }
}
