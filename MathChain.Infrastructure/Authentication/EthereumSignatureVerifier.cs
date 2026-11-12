using System.Text.RegularExpressions;
using MathChain.Domain.Interfaces;
using Nethereum.Signer;
using Nethereum.Util;

namespace MathChain.Infrastructure.Authentication
{
    public class EthereumSignatureVerifier : IWalletSignatureVerifier
    {
        private static readonly Regex AddressPattern = new("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled);

        public bool IsValidAddress(string walletAddress)
        {
            return !string.IsNullOrWhiteSpace(walletAddress) && AddressPattern.IsMatch(walletAddress);
        }

        public string ToChecksumAddress(string walletAddress)
        {
            return new AddressUtil().ConvertToChecksumAddress(walletAddress);
        }

        public bool IsSignedBy(string message, string signature, string walletAddress)
        {
            try
            {
                var recoveredAddress = new EthereumMessageSigner().EncodeUTF8AndEcRecover(message, signature);
                return string.Equals(recoveredAddress, walletAddress, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
