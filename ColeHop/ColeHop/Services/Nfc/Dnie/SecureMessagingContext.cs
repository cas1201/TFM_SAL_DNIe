using ColeHop.Helpers;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class SecureMessagingContext : IDisposable
    {
        private readonly byte[] _kEnc;
        private readonly byte[] _kMac;
        private readonly byte[] _ssc;
        private bool _disposed;

        public SecureMessagingContext(byte[] kEnc, byte[] kMac, byte[] initialSsc)
        {
            _kEnc = kEnc;
            _kMac = kMac;
            _ssc = (byte[])initialSsc.Clone();
        }

        public byte[] ProtectApdu(byte[] plainApdu)
        {
            if (plainApdu.Length < 4)
                throw new ArgumentException("APDU demasiado corta.");

            byte cla = plainApdu[0];
            byte ins = plainApdu[1];
            byte p1 = plainApdu[2];
            byte p2 = plainApdu[3];

            byte[]? commandData = null;
            byte le = 0;
            bool hasLe = false;

            if (plainApdu.Length == 5)
            {
                le = plainApdu[4];
                hasLe = true;
            }
            else if (plainApdu.Length > 5)
            {
                int lc = plainApdu[4];
                commandData = new byte[lc];
                Array.Copy(plainApdu, 5, commandData, 0, lc);

                if (plainApdu.Length > 5 + lc)
                {
                    le = plainApdu[5 + lc];
                    hasLe = true;
                }
            }
            else if (plainApdu.Length == 4)
            {
                hasLe = false;
            }

            ByteExtensions.IncrementBigEndian(_ssc);

            var doList = new List<byte>();

            // DO87: datos cifrados
            if (commandData != null && commandData.Length > 0)
            {
                var encData = EncryptData(commandData);
                doList.Add(0x87);
                doList.AddRange(Asn1Utils.EncodeTlvLength(encData.Length + 1));
                doList.Add(0x01);
                doList.AddRange(encData);
            }

            // DO97: Le esperado
            if (hasLe)
            {
                doList.Add(0x97);
                doList.Add(0x01);
                doList.Add(le);
            }

            // Calcular MAC
            var macInput = BuildMacInput((byte)(cla | 0x0C), ins, p1, p2, doList.ToArray());
            var mac = CalculateCmac(macInput).Take(8).ToArray();

            // DO8E: MAC
            doList.Add(0x8E);
            doList.Add(0x08);
            doList.AddRange(mac);

            // Construir APDU protegida
            var protectedApdu = new List<byte>
            {
                (byte)(cla | 0x0C), ins, p1, p2
            };

            if (doList.Count > 255)
            {
                protectedApdu.Add(0x00);
                protectedApdu.Add((byte)(doList.Count >> 8));
                protectedApdu.Add((byte)(doList.Count & 0xFF));
            }
            else
            {
                protectedApdu.Add((byte)doList.Count);
            }

            protectedApdu.AddRange(doList);
            protectedApdu.Add(0x00);

            return protectedApdu.ToArray();
        }

        public byte[] UnprotectResponse(byte[] protectedResponse)
        {
            if (protectedResponse.Length < 2)
                throw new InvalidOperationException("Respuesta protegida demasiado corta.");

            var sw1 = protectedResponse[^2];
            var sw2 = protectedResponse[^1];

            if (protectedResponse.Length == 2)
                return protectedResponse;

            var doData = new byte[protectedResponse.Length - 2];
            Array.Copy(protectedResponse, 0, doData, 0, doData.Length);

            byte[]? encryptedData = null;
            byte[]? receivedMac = null;
            byte responseSw1 = sw1;
            byte responseSw2 = sw2;

            int offset = 0;
            while (offset < doData.Length)
            {
                byte tag = doData[offset++];
                int length = Asn1Utils.ParseTlvLength(doData, offset, out int lenBytes);
                offset += lenBytes;

                if (tag == 0x87)
                {
                    offset++;
                    encryptedData = new byte[length - 1];
                    Array.Copy(doData, offset, encryptedData, 0, encryptedData.Length);
                    offset += encryptedData.Length;
                }
                else if (tag == 0x99)
                {
                    responseSw1 = doData[offset];
                    responseSw2 = doData[offset + 1];
                    offset += length;
                }
                else if (tag == 0x8E)
                {
                    receivedMac = new byte[length];
                    Array.Copy(doData, offset, receivedMac, 0, length);
                    offset += length;
                }
                else
                {
                    offset += length;
                }
            }

            ByteExtensions.IncrementBigEndian(_ssc);

            // Verificar MAC
            if (receivedMac != null)
            {
                var macData = BuildResponseMacInput(doData);
                var expectedMac = CalculateCmac(macData).Take(8).ToArray();

                if (!receivedMac.SequenceEqual(expectedMac))
                    throw new InvalidOperationException("MAC de respuesta SM invalido.");
            }

            // Descifrar datos
            if (encryptedData == null || encryptedData.Length == 0)
                return [responseSw1, responseSw2];

            var plainData = DecryptData(encryptedData);
            var unpadded = plainData.RemoveIso7816Padding();

            var result = new byte[unpadded.Length + 2];
            Array.Copy(unpadded, 0, result, 0, unpadded.Length);
            result[^2] = responseSw1;
            result[^1] = responseSw2;

            return result;
        }

        private byte[] EncryptData(byte[] plainData)
        {
            var padded = plainData.ApplyIso7816Padding();

            var cipher = CipherUtilities.GetCipher("AES/CBC/NoPadding");
            var iv = ComputeIv();
            cipher.Init(true, new ParametersWithIV(new KeyParameter(_kEnc), iv));

            return cipher.DoFinal(padded);
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            var cipher = CipherUtilities.GetCipher("AES/CBC/NoPadding");
            var iv = ComputeIv();
            cipher.Init(false, new ParametersWithIV(new KeyParameter(_kEnc), iv));

            return cipher.DoFinal(encryptedData);
        }

        private byte[] ComputeIv()
        {
            var cipher = CipherUtilities.GetCipher("AES/ECB/NoPadding");
            cipher.Init(true, new KeyParameter(_kEnc));
            return cipher.DoFinal(_ssc);
        }

        private byte[] BuildMacInput(byte cla, byte ins, byte p1, byte p2, byte[] doBytes)
        {
            var input = new List<byte>();
            input.AddRange(_ssc);

            var header = new byte[] { cla, ins, p1, p2 };
            input.AddRange(header.ApplyIso7816Padding());

            if (doBytes.Length > 0)
                input.AddRange(doBytes.ApplyIso7816Padding());

            return input.ToArray();
        }

        private byte[] BuildResponseMacInput(byte[] doData)
        {
            var input = new List<byte>();
            input.AddRange(_ssc);

            // Incluir todos los DOs excepto 8E (MAC)
            int offset = 0;
            var dataWithoutMac = new List<byte>();

            while (offset < doData.Length)
            {
                byte tag = doData[offset];
                int tagStart = offset;
                offset++;
                int length = Asn1Utils.ParseTlvLength(doData, offset, out int lenBytes);
                offset += lenBytes;

                if (tag != 0x8E)
                {
                    int totalLen = (offset + length) - tagStart;
                    for (int i = tagStart; i < tagStart + totalLen; i++)
                        dataWithoutMac.Add(doData[i]);
                }

                offset += length;
            }

            input.AddRange(dataWithoutMac.ToArray().ApplyIso7816Padding());
            return input.ToArray();
        }

        private byte[] CalculateCmac(byte[] data)
        {
            var mac = new CMac(new Org.BouncyCastle.Crypto.Engines.AesEngine(), 128);
            mac.Init(new KeyParameter(_kMac));
            mac.BlockUpdate(data, 0, data.Length);

            var output = new byte[mac.GetMacSize()];
            mac.DoFinal(output, 0);
            return output;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Array.Clear(_kEnc);
            Array.Clear(_kMac);
            Array.Clear(_ssc);
        }
    }
}
