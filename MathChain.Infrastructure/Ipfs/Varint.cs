namespace MathChain.Infrastructure.Ipfs
{
    public static class Varint
    {
        public const int MaximumLength = 9;

        public static byte[] Encode(ulong value)
        {
            var buffer = new List<byte>(MaximumLength);

            do
            {
                var current = (byte)(value & 0x7F);
                value >>= 7;

                if (value != 0)
                {
                    current |= 0x80;
                }

                buffer.Add(current);
            }
            while (value != 0);

            return buffer.ToArray();
        }

        public static void Write(List<byte> output, ulong value)
        {
            output.AddRange(Encode(value));
        }

        public static bool TryRead(ReadOnlySpan<byte> input, out ulong value, out int length)
        {
            value = 0;
            length = 0;

            for (var shift = 0; length < input.Length && length < MaximumLength; shift += 7)
            {
                var current = input[length++];
                value |= (ulong)(current & 0x7F) << shift;

                if ((current & 0x80) == 0)
                {
                    return length == 1 || current != 0;
                }
            }

            value = 0;
            length = 0;

            return false;
        }
    }
}
