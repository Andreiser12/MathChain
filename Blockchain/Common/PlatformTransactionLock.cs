namespace MathChain.Blockchain.Common
{
    public class PlatformTransactionLock
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
