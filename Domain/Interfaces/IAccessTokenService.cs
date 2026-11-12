using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface IAccessTokenService
    {
        AccessToken CreateToken(User user);
    }
}
