namespace ColeHop.Helpers
{
    public static class ByteExtensions
    {
        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }

        public static byte[] ApplyIso7816Padding(this byte[] data, int blockSize = 16)
        {
            int paddingLength = blockSize - ((data.Length + 1) % blockSize) + 1;
            var padded = new byte[data.Length + paddingLength];
            Array.Copy(data, 0, padded, 0, data.Length);
            padded[data.Length] = 0x80;
            return padded;
        }

        public static byte[] RemoveIso7816Padding(this byte[] data)
        {
            for (int i = data.Length - 1; i >= 0; i--)
            {
                if (data[i] == 0x80)
                {
                    var result = new byte[i];
                    Array.Copy(data, 0, result, 0, i);
                    return result;
                }

                if (data[i] != 0x00)
                    throw new InvalidOperationException("Padding ISO 7816-4 inválido.");
            }

            throw new InvalidOperationException("Padding ISO 7816-4 no encontrado.");
        }

        public static void IncrementBigEndian(byte[] counter)
        {
            for (int i = counter.Length - 1; i >= 0; i--)
            {
                if (++counter[i] != 0)
                    break;
            }
        }
    }
}
