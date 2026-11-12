namespace MathChain.Domain.Interfaces
{
    public interface IWalletSignatureVerifier
    {
        bool IsValidAddress(string walletAddress);
        string ToChecksumAddress(string walletAddress);
        bool IsSignedBy(string message, string signature, string walletAddress);
    }
}
