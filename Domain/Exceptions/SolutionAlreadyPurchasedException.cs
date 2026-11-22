namespace MathChain.Domain.Exceptions
{
    public class SolutionAlreadyPurchasedException : BusinessRuleException
    {
        public SolutionAlreadyPurchasedException() : base("This solution has already been paid for.")
        {
        }
    }
}
