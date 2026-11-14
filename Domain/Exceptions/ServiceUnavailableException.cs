namespace MathChain.Domain.Exceptions
{
    public class ServiceUnavailableException : DomainException
    {
        public ServiceUnavailableException(string message) : base(message)
        {
        }
    }
}
