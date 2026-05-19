using ColeHop.Helpers;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class PaceSession : IDisposable
    {
        private readonly INfcPlatformService _platformService;
        private static readonly X9ECParameters BrainpoolP256r1 = ECNamedCurveTable.GetByName("brainpoolP256r1");
        private static readonly ECDomainParameters DomainParams = new(
            BrainpoolP256r1.Curve, BrainpoolP256r1.G, BrainpoolP256r1.N, BrainpoolP256r1.H);

        private bool _disposed;

        public byte[] KEnc { get; private set; } = [];
        public byte[] KMac { get; private set; } = [];
        public byte[] InitialSsc { get; private set; } = [];

        public PaceSession(INfcPlatformService platformService)
        {
            _platformService = platformService;
        }

        public async Task EstablishSecureChannelAsync(string can, CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[PACE] Iniciando protocolo PACE...");

            // Seleccionar Master File antes de PACE (requerido por DNIe 3.0)
            await SelectMasterFileAsync(cancellationToken);

            // Leer EF.CardAccess para descubrir el OID PACE real de la tarjeta
            var cardAccess = await ReadEfCardAccessAsync(cancellationToken);

            var kPi = DeriveKeyFromCan(can);

            await SendMseSetAtAsync(cancellationToken);

            var encryptedNonce = await RequestEncryptedNonceAsync(cancellationToken);

            var nonce = DecryptNonce(encryptedNonce, kPi);

            // Paso 3: Generic Mapping – primera ronda ECDH sobre G original
            var (mapTermPub, mapTermPriv) = GenerateEcdhKeyPair(DomainParams);
            var mapChipPub = await ExchangeMappingKeysAsync(mapTermPub, cancellationToken);
            // H = punto ECDH compartido (punto completo, NO solo coordenada X)
            var H = mapChipPub.Q.Multiply(mapTermPriv.D).Normalize();

            // G' = s*G + H (Generic Mapping según BSI TR-03110)
            var s = new BigInteger(1, nonce);
            var sG = DomainParams.G.Multiply(s).Normalize();
            var mappedGenerator = sG.Add(H).Normalize();

            var mappedDomain = new ECDomainParameters(
                DomainParams.Curve, mappedGenerator, DomainParams.N, DomainParams.H);

            var (terminalPubKey, terminalPrivKey) = GenerateEcdhKeyPair(mappedDomain);

            var chipPubKey = await ExchangeEphemeralKeysAsync(terminalPubKey, mappedDomain, cancellationToken);

            var sharedSecret = ComputeSharedSecret(terminalPrivKey, chipPubKey);

            var (kEnc, kMac) = DeriveSessionKeys(sharedSecret);
            KEnc = kEnc;
            KMac = kMac;

            await PerformMutualAuthenticationAsync(kMac, terminalPubKey, chipPubKey, cancellationToken);

            // BSI TR-03110-3 A.2.4: Para PACE con AES, el SSC se inicializa a cero
            InitialSsc = new byte[16];

            System.Diagnostics.Debug.WriteLine("[PACE] Autenticacion mutua exitosa - Canal seguro establecido");
        }

        private async Task SelectMasterFileAsync(CancellationToken cancellationToken)
        {
            // SELECT MF (3F00)
            var apdu = new byte[] { 0x00, 0xA4, 0x00, 0x0C, 0x02, 0x3F, 0x00 };
            await TransceiveOrThrowAsync(apdu, cancellationToken);
        }

        private async Task<byte[]> ReadEfCardAccessAsync(CancellationToken cancellationToken)
        {
            // SELECT EF.CardAccess (011C)
            var selectApdu = new byte[] { 0x00, 0xA4, 0x02, 0x0C, 0x02, 0x01, 0x1C };
            await TransceiveOrThrowAsync(selectApdu, cancellationToken);

            // READ BINARY
            var data = new List<byte>();
            int offset = 0;
            while (true)
            {
                var readApdu = new byte[]
                {
                    0x00, 0xB0,
                    (byte)(offset >> 8),
                    (byte)(offset & 0xFF),
                    0x00 // Le=256
                };
                var result = await _platformService.TransceiveAsync(readApdu, cancellationToken);
                if (!result.IsValid || result.RawData == null || result.RawData.Length < 2)
                    break;

                var sw = GetStatusWord(result.RawData);
                var chunk = new byte[result.RawData.Length - 2];
                Array.Copy(result.RawData, 0, chunk, 0, chunk.Length);

                if (chunk.Length > 0)
                    data.AddRange(chunk);

                if (sw == 0x9000)
                {
                    offset += chunk.Length;
                    if (chunk.Length == 0) break;
                }
                else
                {
                    break;
                }
            }
            return data.ToArray();
        }

        private static byte[] DeriveKeyFromCan(string can)
        {
            // BSI TR-03110 Table A.1: AES-128 → SHA-1
            // K_pi = SHA-1(CAN_bytes || 00000003)[0:16]
            var digest = new Sha1Digest();
            var input = System.Text.Encoding.ASCII.GetBytes(can);
            digest.BlockUpdate(input, 0, input.Length);
            var counter = new byte[] { 0x00, 0x00, 0x00, 0x03 };
            digest.BlockUpdate(counter, 0, 4);
            var hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);

            return hash.Take(16).ToArray();
        }

        private async Task SendMseSetAtAsync(CancellationToken cancellationToken)
        {
            // OID: id-PACE-ECDH-GM-AES-CBC-CMAC-128 (0.4.0.127.0.7.2.2.4.2.2)
            var oidPace = new byte[] { 0x04, 0x00, 0x7F, 0x00, 0x07, 0x02, 0x02, 0x04, 0x02, 0x02 };

            var data = new List<byte>();
            // Tag 80: OID del algoritmo PACE
            data.Add(0x80);
            data.Add((byte)oidPace.Length);
            data.AddRange(oidPace);
            // Tag 83: Password reference (02 = CAN)
            data.Add(0x83);
            data.Add(0x01);
            data.Add(0x02);
            // Tag 84: Standardized Domain Parameter ID (0D = brainpoolP256r1)
            data.Add(0x84);
            data.Add(0x01);
            data.Add(0x0D);

            var apdu = new List<byte> { 0x00, 0x22, 0xC1, 0xA4, (byte)data.Count };
            apdu.AddRange(data);

            await TransceiveOrThrowAsync(apdu.ToArray(), cancellationToken);
        }

        private async Task<byte[]> RequestEncryptedNonceAsync(CancellationToken cancellationToken)
        {
            var apdu = new byte[] { 0x10, 0x86, 0x00, 0x00, 0x02, 0x7C, 0x00, 0x00 };
            var response = await TransceiveAndReturnDataAsync(apdu, cancellationToken);
            return ParseTlvContent(response, 0x80);
        }

        private static byte[] DecryptNonce(byte[] encryptedNonce, byte[] kPi)
        {
            var cipher = CipherUtilities.GetCipher("AES/CBC/NoPadding");
            var keyParam = new KeyParameter(kPi);
            var ivParam = new ParametersWithIV(keyParam, new byte[16]);
            cipher.Init(false, ivParam);
            return cipher.DoFinal(encryptedNonce);
        }

        private async Task<ECPublicKeyParameters> ExchangeMappingKeysAsync(
            ECPublicKeyParameters terminalPubKey,
            CancellationToken cancellationToken)
        {
            var pkBytes = terminalPubKey.Q.GetEncoded(false);

            var innerTlv = new List<byte> { 0x81 };
            innerTlv.AddRange(Asn1Utils.EncodeTlvLength(pkBytes.Length));
            innerTlv.AddRange(pkBytes);

            var outerTlv = new List<byte> { 0x7C };
            outerTlv.AddRange(Asn1Utils.EncodeTlvLength(innerTlv.Count));
            outerTlv.AddRange(innerTlv);

            var apdu = new List<byte> { 0x10, 0x86, 0x00, 0x00 };
            apdu.AddRange(Asn1Utils.EncodeTlvLength(outerTlv.Count));
            apdu.AddRange(outerTlv);
            apdu.Add(0x00);

            var response = await TransceiveAndReturnDataAsync(apdu.ToArray(), cancellationToken);
            var chipPkBytes = ParseTlvContent(response, 0x82);

            var chipPoint = DomainParams.Curve.DecodePoint(chipPkBytes);
            return new ECPublicKeyParameters("ECDH", chipPoint, DomainParams);
        }

        private static (ECPublicKeyParameters, ECPrivateKeyParameters) GenerateEcdhKeyPair(ECDomainParameters domain)
        {
            var gen = new ECKeyPairGenerator();
            gen.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
            var keyPair = gen.GenerateKeyPair();
            return ((ECPublicKeyParameters)keyPair.Public, (ECPrivateKeyParameters)keyPair.Private);
        }

        private async Task<ECPublicKeyParameters> ExchangeEphemeralKeysAsync(
            ECPublicKeyParameters terminalPubKey,
            ECDomainParameters mappedDomain,
            CancellationToken cancellationToken)
        {
            var pkBytes = terminalPubKey.Q.GetEncoded(false);

            var innerTlv = new List<byte> { 0x83 };
            innerTlv.AddRange(Asn1Utils.EncodeTlvLength(pkBytes.Length));
            innerTlv.AddRange(pkBytes);

            var outerTlv = new List<byte> { 0x7C };
            outerTlv.AddRange(Asn1Utils.EncodeTlvLength(innerTlv.Count));
            outerTlv.AddRange(innerTlv);

            var apdu = new List<byte> { 0x10, 0x86, 0x00, 0x00 };
            apdu.AddRange(Asn1Utils.EncodeTlvLength(outerTlv.Count));
            apdu.AddRange(outerTlv);
            apdu.Add(0x00);

            var response = await TransceiveAndReturnDataAsync(apdu.ToArray(), cancellationToken);
            var chipPkBytes = ParseTlvContent(response, 0x84);

            var chipPoint = mappedDomain.Curve.DecodePoint(chipPkBytes);
            return new ECPublicKeyParameters("ECDH", chipPoint, mappedDomain);
        }

        private static byte[] ComputeSharedSecret(ECPrivateKeyParameters privateKey, ECPublicKeyParameters chipPublicKey)
        {
            // Usar ECDHBasicAgreement para calcular el secreto compartido (coordenada X)
            var agreement = new ECDHBasicAgreement();
            agreement.Init(privateKey);
            var sharedSecretBigInt = agreement.CalculateAgreement(chipPublicKey);
            // Resultado como array de 32 bytes (tamaño del campo para brainpoolP256r1)
            return BigIntegers.AsUnsignedByteArray(
                (DomainParams.Curve.FieldSize + 7) / 8, sharedSecretBigInt);
        }

        private static (byte[] kEnc, byte[] kMac) DeriveSessionKeys(byte[] sharedSecret)
        {
            var kEnc = KdfCounter(sharedSecret, 1);
            var kMac = KdfCounter(sharedSecret, 2);
            return (kEnc, kMac);
        }

        private static byte[] KdfCounter(byte[] sharedSecret, int counter)
        {
            // KDF: SHA-1(sharedSecret || counter)[0:16]
            // BSI TR-03110 Table A.1: AES-128 → SHA-1
            var digest = new Sha1Digest();
            digest.BlockUpdate(sharedSecret, 0, sharedSecret.Length);

            var counterBytes = new byte[4];
            counterBytes[0] = (byte)(counter >> 24);
            counterBytes[1] = (byte)(counter >> 16);
            counterBytes[2] = (byte)(counter >> 8);
            counterBytes[3] = (byte)counter;
            digest.BlockUpdate(counterBytes, 0, 4);

            var output = new byte[digest.GetDigestSize()];
            digest.DoFinal(output, 0);
            return output.Take(16).ToArray();
        }

        private async Task PerformMutualAuthenticationAsync(
            byte[] kMac,
            ECPublicKeyParameters terminalPubKey,
            ECPublicKeyParameters chipPubKey,
            CancellationToken cancellationToken)
        {
            var pkTermBytes = terminalPubKey.Q.GetEncoded(false);
            var pkChipBytes = chipPubKey.Q.GetEncoded(false);

            // OID id-PACE-ECDH-GM-AES-CBC-CMAC-128: 0.4.0.127.0.7.2.2.4.2.2
            var oid = new byte[] { 0x04, 0x00, 0x7F, 0x00, 0x07, 0x02, 0x02, 0x04, 0x02, 0x02 };

            // Según BSI TR-03110: el terminal calcula MAC sobre el auth token del chip
            var authTokenChip = BuildAuthToken(oid, pkChipBytes);
            var macTerminal = CalculateCmac(kMac, authTokenChip).Take(8).ToArray();

            var innerTlv = new List<byte> { 0x85, 0x08 };
            innerTlv.AddRange(macTerminal);

            var outerTlv = new List<byte> { 0x7C };
            outerTlv.AddRange(Asn1Utils.EncodeTlvLength(innerTlv.Count));
            outerTlv.AddRange(innerTlv);

            var apdu = new List<byte> { 0x00, 0x86, 0x00, 0x00, (byte)outerTlv.Count };
            apdu.AddRange(outerTlv);
            apdu.Add(0x00);

            var response = await TransceiveAndReturnDataAsync(apdu.ToArray(), cancellationToken);
            var macChip = ParseTlvContent(response, 0x86);

            // El chip calcula MAC sobre el auth token del terminal
            var authTokenTerm = BuildAuthToken(oid, pkTermBytes);
            var expectedMac = CalculateCmac(kMac, authTokenTerm).Take(8).ToArray();

            if (!macChip.SequenceEqual(expectedMac))
                throw new InvalidOperationException("Autenticacion mutua fallida: MAC del chip invalido.");
        }

        private static byte[] BuildAuthToken(byte[] oid, byte[] publicKeyBytes)
        {
            // Formato BSI TR-03110: 7F49 [ 06 OID, 86 PK ]
            var oidTlv = new List<byte> { 0x06, (byte)oid.Length };
            oidTlv.AddRange(oid);

            var pkTlv = new List<byte> { 0x86 };
            pkTlv.AddRange(Asn1Utils.EncodeTlvLength(publicKeyBytes.Length));
            pkTlv.AddRange(publicKeyBytes);

            var innerContent = new List<byte>();
            innerContent.AddRange(oidTlv);
            innerContent.AddRange(pkTlv);

            var result = new List<byte> { 0x7F, 0x49 };
            result.AddRange(Asn1Utils.EncodeTlvLength(innerContent.Count));
            result.AddRange(innerContent);

            return result.ToArray();
        }

        private static byte[] CalculateCmac(byte[] key, byte[] data)
        {
            var mac = new CMac(new Org.BouncyCastle.Crypto.Engines.AesEngine(), 128);
            mac.Init(new KeyParameter(key));
            mac.BlockUpdate(data, 0, data.Length);

            var output = new byte[mac.GetMacSize()];
            mac.DoFinal(output, 0);
            return output;
        }

        private static byte[] ParseTlvContent(byte[] data, byte targetTag)
        {
            int offset = 0;

            // Si empieza con 7C (Dynamic Authentication Data), entrar
            if (data.Length > 2 && data[0] == 0x7C)
            {
                offset = 1;
                Asn1Utils.ParseTlvLength(data, offset, out int lenBytes);
                offset += lenBytes;
            }

            while (offset < data.Length)
            {
                byte tag = data[offset++];
                int length = Asn1Utils.ParseTlvLength(data, offset, out int consumed);
                offset += consumed;

                if (tag == targetTag)
                {
                    var result = new byte[length];
                    Array.Copy(data, offset, result, 0, length);
                    return result;
                }

                offset += length;
            }

            throw new InvalidOperationException($"Tag {targetTag:X2} no encontrado en respuesta TLV.");
        }

        private async Task TransceiveOrThrowAsync(byte[] apdu, CancellationToken cancellationToken)
        {
            var result = await _platformService.TransceiveAsync(apdu, cancellationToken);

            if (!result.IsValid || result.RawData == null)
                throw new InvalidOperationException(result.ErrorMessage ?? "Error NFC durante PACE.");

            var sw = GetStatusWord(result.RawData);
            if (sw != 0x9000 && sw != 0x9001)
                throw new InvalidOperationException($"PACE error SW={sw:X4}");
        }

        private async Task<byte[]> TransceiveAndReturnDataAsync(byte[] apdu, CancellationToken cancellationToken)
        {
            var result = await _platformService.TransceiveAsync(apdu, cancellationToken);

            if (!result.IsValid || result.RawData == null)
                throw new InvalidOperationException(result.ErrorMessage ?? "Error NFC durante PACE.");

            var raw = result.RawData;
            if (raw.Length < 2)
                throw new InvalidOperationException("Respuesta NFC demasiado corta.");

            var sw = GetStatusWord(raw);
            if (sw != 0x9000 && sw != 0x9001)
                throw new InvalidOperationException($"PACE error SW={sw:X4}");

            var data = new byte[raw.Length - 2];
            Array.Copy(raw, 0, data, 0, data.Length);
            return data;
        }

        private static ushort GetStatusWord(byte[] response)
        {
            if (response.Length < 2) return 0;
            return (ushort)((response[^2] << 8) | response[^1]);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (KEnc.Length > 0) Array.Clear(KEnc);
            if (KMac.Length > 0) Array.Clear(KMac);
            KEnc = [];
            KMac = [];
        }
    }
}
