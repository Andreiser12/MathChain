namespace MathChain.Blockchain.Custody
{
    public class CustodySettings
    {
        public const int MinimumSecretSizeInBytes = 32;

        public string MasterSecret { get; set; } = string.Empty;
    }
}
