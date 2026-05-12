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
    public sealed class PaceSession
    {
        private readonly INfcPlatformService _platformService;
        private static readonly X9ECParameters BrainpoolP256r1 = ECNamedCurveTable.GetByName("brainpoolP256r1");
        private static readonly ECDomainParameters DomainParams = new(
            BrainpoolP256r1.Curve, BrainpoolP256r1.G, BrainpoolP256r1.N, BrainpoolP256r1.H);

        public byte[] KEnc { get; private set; } = [];
        public byte[] KMac { get; private set; } = [];

        public PaceSession(INfcPlatformService platformService)
        {
            _platformService = platformService;
        }

        public async Task EstablishSecureChannelAsync(string can, CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[PACE] Iniciando protocolo PACE...");

            var kPi = DeriveKeyFromCan(can);
            System.Diagnostics.Debug.WriteLine($"[PACE] K_pi derivada del CAN ({can.Length} chars)");

            await SendMseSetAtAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine("[PACE] MSE:Set AT enviado correctamente");

            var encryptedNonce = await RequestEncryptedNonceAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[PACE] Nonce cifrado recibido: {encryptedNonce.Length} bytes");

            var nonce = DecryptNonce(encryptedNonce, kPi);
            System.Diagnostics.Debug.WriteLine($"[PACE] Nonce descifrado: {nonce.Length} bytes");

            var mappedGenerator = MapGenerator(nonce);
            System.Diagnostics.Debug.WriteLine("[PACE] Generador mapeado G' calculado");

            var mappedDomain = new ECDomainParameters(
                DomainParams.Curve, mappedGenerator, DomainParams.N, DomainParams.H);

            var (terminalPubKey, terminalPrivKey) = GenerateEcdhKeyPair(mappedDomain);
            System.Diagnostics.Debug.WriteLine("[PACE] Par ECDH terminal generado sobre G'");

            var chipPubKey = await ExchangeEphemeralKeysAsync(terminalPubKey, mappedDomain, cancellationToken);
            System.Diagnostics.Debug.WriteLine("[PACE] Intercambio de claves efimeras completado");

            var sharedSecret = ComputeSharedSecret(terminalPrivKey, chipPubKey);
            System.Diagnostics.Debug.WriteLine($"[PACE] Secreto compartido: {sharedSecret.Length} bytes");

            var (kEnc, kMac) = DeriveSessionKeys(sharedSecret);
            KEnc = kEnc;
            KMac = kMac;
            System.Diagnostics.Debug.WriteLine($"[PACE] K_enc: {kEnc.Length} bytes, K_mac: {kMac.Length} bytes");

            await PerformMutualAuthenticationAsync(kMac, terminalPubKey, chipPubKey, cancellationToken);
            System.Diagnostics.Debug.WriteLine("[PACE] Autenticacion mutua exitosa - Canal seguro establecido");
        }

        private static byte[] DeriveKeyFromCan(string can)
        {
            var digest = new Sha256Digest();
            var input = System.Text.Encoding.ASCII.GetBytes(can);
            digest.BlockUpdate(input, 0, input.Length);

            var hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);

            return hash.Take(16).ToArray();
        }

        private async Task SendMseSetAtAsync(CancellationToken cancellationToken)
        {
            // OID: id-PACE-ECDH-GM-AES-CBC-CMAC-128 (0.4.0.127.0.7.2.2.4.2.2)
            var oidPace = new byte[] { 0x04, 0x00, 0x7F, 0x00, 0x07, 0x02, 0x02, 0x04, 0x02, 0x02 };

            var data = new List<byte>();
            data.Add(0x80);
            data.Add((byte)oidPace.Length);
            data.AddRange(oidPace);
            data.Add(0x83);
            data.Add(0x01);
            data.Add(0x02);

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

        private static ECPoint MapGenerator(byte[] nonce)
        {
            var s = new BigInteger(1, nonce);
            return DomainParams.G.Multiply(s).Normalize();
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

            var innerTlv = new List<byte> { 0x81, (byte)pkBytes.Length };
            innerTlv.AddRange(pkBytes);

            var outerTlv = new List<byte> { 0x7C };
            outerTlv.AddRange(Asn1Utils.EncodeTlvLength(innerTlv.Count));
            outerTlv.AddRange(innerTlv);

            var apdu = new List<byte> { 0x10, 0x86, 0x00, 0x00, (byte)outerTlv.Count };
            apdu.AddRange(outerTlv);
            apdu.Add(0x00);

            var response = await TransceiveAndReturnDataAsync(apdu.ToArray(), cancellationToken);
            var chipPkBytes = ParseTlvContent(response, 0x82);

            var chipPoint = mappedDomain.Curve.DecodePoint(chipPkBytes);
            return new ECPublicKeyParameters("ECDH", chipPoint, mappedDomain);
        }

        private static byte[] ComputeSharedSecret(ECPrivateKeyParameters privateKey, ECPublicKeyParameters chipPublicKey)
        {
            var agreement = new ECDHBasicAgreement();
            agreement.Init(privateKey);
            var secret = agreement.CalculateAgreement(chipPublicKey);
            return BigIntegers.AsUnsignedByteArray(32, secret);
        }

        private static (byte[] kEnc, byte[] kMac) DeriveSessionKeys(byte[] sharedSecret)
        {
            var kEnc = KdfCounter(sharedSecret, 1);
            var kMac = KdfCounter(sharedSecret, 2);
            return (kEnc, kMac);
        }

        private static byte[] KdfCounter(byte[] sharedSecret, int counter)
        {
            var digest = new Sha256Digest();
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

            // MAC del terminal sobre PK_chip || PK_terminal
            var dataToMac = pkChipBytes.Concat(pkTermBytes).ToArray();
            var macTerminal = CalculateCmac(kMac, dataToMac).Take(8).ToArray();

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

            // Verificar MAC del chip sobre PK_terminal || PK_chip
            var dataToMacChip = pkTermBytes.Concat(pkChipBytes).ToArray();
            var expectedMac = CalculateCmac(kMac, dataToMacChip).Take(8).ToArray();

            if (!macChip.SequenceEqual(expectedMac))
                throw new InvalidOperationException("Autenticacion mutua fallida: MAC del chip invalido.");
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
    }
}
