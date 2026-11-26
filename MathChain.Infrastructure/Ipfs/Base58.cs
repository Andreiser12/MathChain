using System.Numerics;

namespace MathChain.Infrastructure.Ipfs
{
    public static class Base58
    {
        private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string Encode(ReadOnlySpan<byte> data)
        {
            var leadingZeros = 0;

            while (leadingZeros < data.Length && data[leadingZeros] == 0)
            {
                leadingZeros++;
            }

            var value = new BigInteger(data, isUnsigned: true, isBigEndian: true);
            var output = new System.Text.StringBuilder();

            while (value > 0)
            {
                value = BigInteger.DivRem(value, 58, out var remainder);
                output.Insert(0, Alphabet[(int)remainder]);
            }

            return new string('1', leadingZeros) + output;
        }

        public static bool TryDecode(string text, out byte[] data)
        {
            BigInteger value = 0;

            foreach (var character in text)
            {
                var index = Alphabet.IndexOf(character);

                if (index < 0)
                {
                    data = Array.Empty<byte>();
                    return false;
                }

                value = value * 58 + index;
            }

            var leadingZeros = text.TakeWhile(c => c == '1').Count();
            var body = value.IsZero ? Array.Empty<byte>() : value.ToByteArray(isUnsigned: true, isBigEndian: true);

            data = new byte[leadingZeros + body.Length];
            body.CopyTo(data, leadingZeros);

            return true;
        }
    }
}
