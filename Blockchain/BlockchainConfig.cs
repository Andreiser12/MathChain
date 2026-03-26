namespace MathChain.Blockchain
{
    public class BlockchainConfig
    {
        public string RpcUrl { get; set; } = Environment.GetEnvironmentVariable("INFURA_RPC_URL");
        public string ContractAddress { get; set; } = Environment.GetEnvironmentVariable("CONTRACT_ADDRESS");
        public string PrivateKey { get; set; } = Environment.GetEnvironmentVariable("METAMASK_PRIVATE_KEY");
    }
}
