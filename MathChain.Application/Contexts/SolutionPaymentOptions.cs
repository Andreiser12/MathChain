namespace MathChain.Application.Contexts
{
    public class SolutionPaymentOptions
    {
        public TimeSpan QuoteLifetime { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan ConfirmationWait { get; set; } = TimeSpan.FromSeconds(40);
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);
        public decimal MaximumPrice { get; set; } = 1000m;
    }
}
