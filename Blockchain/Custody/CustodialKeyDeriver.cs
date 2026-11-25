using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using MathChain.Domain.Interfaces;
using Nethereum.Signer;

namespace MathChain.Blockchain.Custody
{
    public class CustodialKeyDeriver : ICustodialAddressProvider, ICustodialSigner
    {
        private const int PrivateKeySizeInBytes = 32;
        private const string KeyPurpose = "mathchain/owner-key/v1";

        private static readonly BigInteger CurveOrder = BigInteger.Parse(
            "00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141",
            System.Globalization.NumberStyles.HexNumber);

        private readonly byte[] _masterSecret;

        public CustodialKeyDeriver(CustodySettings settings)
        {
            _masterSecret = Encoding.UTF8.GetBytes(settings.MasterSecret ?? string.Empty);
        }

        public bool IsConfigured => _masterSecret.Length >= CustodySettings.MinimumSecretSizeInBytes;

        public string GetOwnerAddress(Guid userId)
        {
            var key = new EthECKey(DerivePrivateKey(userId), true);

            return key.GetPublicAddress().ToLowerInvariant();
        }

        public string SignPersonalHash(Guid userId, byte[] hash)
        {
            if (hash == null || hash.Length != PrivateKeySizeInBytes)
            {
                throw new ArgumentException("Only 32 byte hashes can be signed.", nameof(hash));
            }

            var key = new EthECKey(DerivePrivateKey(userId), true);

            return new EthereumMessageSigner().Sign(hash, key);
        }

        private byte[] DerivePrivateKey(Guid userId)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    $"Custody:MasterSecret must be configured with at least {CustodySettings.MinimumSecretSizeInBytes} bytes.");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("A user identifier is required to derive an owner key.", nameof(userId));
            }

            for (var attempt = 0; ; attempt++)
            {
                var info = Encoding.UTF8.GetBytes($"{KeyPurpose}/{userId:N}/{attempt}");
                var candidate = HKDF.DeriveKey(HashAlgorithmName.SHA256, _masterSecret, PrivateKeySizeInBytes, salt: null, info: info);
                var scalar = new BigInteger(candidate, isUnsigned: true, isBigEndian: true);

                if (scalar > BigInteger.Zero && scalar < CurveOrder)
                {
                    return candidate;
                }
            }
        }
    }
}
