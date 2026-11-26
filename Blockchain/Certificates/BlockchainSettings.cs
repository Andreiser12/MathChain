namespace MathChain.Blockchain.Certificates
{
    public class BlockchainSettings
    {
        public string NetworkName { get; set; } = "Base Sepolia";
        public string RpcUrl { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string ExplorerUrl { get; set; } = string.Empty;
        public string CertificatesAddress { get; set; } = string.Empty;
        public string SolutionsAddress { get; set; } = string.Empty;
        public string PaymentTokenAddress { get; set; } = string.Empty;
        public string PaymentTokenName { get; set; } = "USDC";
        public string PaymentTokenVersion { get; set; } = "2";
        public string PaymentTokenSymbol { get; set; } = "USDC";
        public int PaymentTokenDecimals { get; set; } = 6;
        public string EntryPointAddress { get; set; } = "0x0000000071727De22E5E9d8BAf0edAc6f37da032";
        public string AccountFactoryAddress { get; set; } = string.Empty;
        public string PaymasterAddress { get; set; } = string.Empty;
        public string CertificateClaimsAddress { get; set; } = string.Empty;
        public string CurriculumRegistryAddress { get; set; } = string.Empty;
        public string IssuerPrivateKey { get; set; } = string.Empty;
        public int ReceiptTimeoutSeconds { get; set; } = 90;
        public int GasPriceMultiplierPercent { get; set; } = 200;
        public int GasLimitMultiplierPercent { get; set; } = 130;
    }
}
