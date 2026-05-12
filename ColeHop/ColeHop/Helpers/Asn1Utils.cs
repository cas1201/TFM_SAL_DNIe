namespace ColeHop.Helpers
{
    public static class Asn1Utils
    {
        public static int ParseTlvLength(byte[] data, int offset, out int bytesConsumed)
        {
            if (offset >= data.Length)
                throw new ArgumentException("Datos insuficientes para parsear longitud TLV.");

            byte first = data[offset];

            if ((first & 0x80) == 0)
            {
                bytesConsumed = 1;
                return first;
            }

            int numBytes = first & 0x7F;

            if (numBytes == 0 || offset + 1 + numBytes > data.Length)
                throw new ArgumentException("Longitud TLV indefinida o datos insuficientes.");

            int length = 0;
            for (int i = 0; i < numBytes; i++)
                length = (length << 8) | data[offset + 1 + i];

            bytesConsumed = 1 + numBytes;
            return length;
        }

        public static int SkipTag(byte[] data, int offset, out int tagLength)
        {
            if (offset >= data.Length)
                throw new ArgumentException("Datos insuficientes para parsear tag.");

            byte first = data[offset];

            if ((first & 0x1F) != 0x1F)
            {
                tagLength = 1;
                return first;
            }

            int i = 1;
            while (offset + i < data.Length && (data[offset + i] & 0x80) != 0)
                i++;

            i++;
            tagLength = i;
            return -1;
        }

        public static byte[] FindTag(byte[] data, byte tag1, byte tag2)
        {
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (data[i] == tag1 && data[i + 1] == tag2)
                {
                    int offset = i + 2;
                    int length = ParseTlvLength(data, offset, out int lenBytes);
                    offset += lenBytes;

                    if (offset + length > data.Length)
                        throw new ArgumentException($"Tag {tag1:X2}{tag2:X2} con longitud fuera de rango.");

                    var result = new byte[length];
                    Array.Copy(data, offset, result, 0, length);
                    return result;
                }
            }

            throw new InvalidOperationException($"Tag {tag1:X2}{tag2:X2} no encontrado.");
        }

        public static byte[] FindTag(byte[] data, byte tag)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == tag)
                {
                    int offset = i + 1;
                    int length = ParseTlvLength(data, offset, out int lenBytes);
                    offset += lenBytes;

                    if (offset + length > data.Length)
                        continue;

                    var result = new byte[length];
                    Array.Copy(data, offset, result, 0, length);
                    return result;
                }
            }

            throw new InvalidOperationException($"Tag {tag:X2} no encontrado.");
        }

        public static byte[] EncodeTlvLength(int length)
        {
            if (length < 0x80)
                return [(byte)length];

            if (length <= 0xFF)
                return [0x81, (byte)length];

            return [0x82, (byte)(length >> 8), (byte)(length & 0xFF)];
        }
    }
}
