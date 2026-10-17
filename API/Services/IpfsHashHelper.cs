using System.Security.Cryptography;

namespace MathChain.API.Services
{
    public static class IpfsHashHelper
    {
        private const string Base58Alphabet =
            "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string ComputeCid(byte[] data)
        {
            byte[] sha256Hash = SHA256.HashData(data);

            byte[] multihash = new byte[34];
            multihash[0] = 0x12;
            multihash[1] = 0x20;
            Buffer.BlockCopy(sha256Hash, 0, multihash, 2, 32);

            return EncodeBase58(multihash);
        }

        private static string EncodeBase58(byte[] data)
        {
            int zeroCount = 0;

            while(zeroCount < data.Length && data[zeroCount] == 0)
            {
                zeroCount++;
            }

            byte[] temp = (byte[])data.Clone();
            char[] result = new char[data.Length * 2];
            int resultIndex = result.Length;

            int startAt = zeroCount;
            while(startAt < temp.Length)
            {
                int remainder = 0;
                for(int i = startAt; i < temp.Length; i++)
                {
                    int current = remainder * 256 + temp[i];
                    temp[i] = (byte)(current / 58);
                    remainder = current % 58;
                }

                if (temp[startAt] == 0)
                {
                    startAt++;
                }

                result[--resultIndex] = Base58Alphabet[remainder];
            }

            while(zeroCount-- > 0)
            {
                result[--resultIndex] = Base58Alphabet[0];
            }

            return new string(result, resultIndex, result.Length - resultIndex);
        }
    }
}
