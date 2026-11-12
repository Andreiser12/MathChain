namespace MathChain.Tests.Fakes
{
    public class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }
}
