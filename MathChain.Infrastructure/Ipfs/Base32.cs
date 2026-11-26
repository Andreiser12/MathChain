namespace MathChain.Infrastructure.Ipfs
{
    public static class Base32
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

        public static string Encode(ReadOnlySpan<byte> data)
        {
            var output = new System.Text.StringBuilder((data.Length * 8 + 4) / 5);
            var buffer = 0;
            var bits = 0;

            foreach (var current in data)
            {
                buffer = (buffer << 8) | current;
                bits += 8;

                while (bits >= 5)
                {
                    output.Append(Alphabet[(buffer >> (bits - 5)) & 0x1F]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                output.Append(Alphabet[(buffer << (5 - bits)) & 0x1F]);
            }

            return output.ToString();
        }

        public static bool TryDecode(string text, out byte[] data)
        {
            var output = new List<byte>(text.Length * 5 / 8);
            var buffer = 0;
            var bits = 0;

            foreach (var character in text)
            {
                var index = Alphabet.IndexOf(character);

                if (index < 0)
                {
                    data = Array.Empty<byte>();
                    return false;
                }

                buffer = ((buffer << 5) | index) & 0xFFF;
                bits += 5;

                if (bits >= 8)
                {
                    output.Add((byte)(buffer >> (bits - 8)));
                    bits -= 8;
                }
            }

            if (bits >= 5 || (buffer & ((1 << bits) - 1)) != 0)
            {
                data = Array.Empty<byte>();
                return false;
            }

            data = output.ToArray();
            return true;
        }
    }
}
