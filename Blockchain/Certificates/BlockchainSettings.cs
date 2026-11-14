namespace MathChain.Blockchain.Certificates
{
    public class BlockchainSettings
    {
        public string NetworkName { get; set; } = "Base Sepolia";
        public string RpcUrl { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string ExplorerUrl { get; set; } = string.Empty;
        public string CertificatesAddress { get; set; } = string.Empty;
        public string IssuerPrivateKey { get; set; } = string.Empty;
        public int ReceiptTimeoutSeconds { get; set; } = 90;
        public int GasPriceMultiplierPercent { get; set; } = 200;
        public int GasLimitMultiplierPercent { get; set; } = 130;
    }
}
