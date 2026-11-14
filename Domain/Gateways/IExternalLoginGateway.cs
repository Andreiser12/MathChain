using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IExternalLoginGateway
    {
        Task<ExternalLogin?> GetAsync(string provider, string subject);
        Task<bool> TryCreateUserWithLoginAsync(User user, ExternalLogin login);
    }
}
