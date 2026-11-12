using MathChain.Domain.Entities;

namespace MathChain.Application.DTOs
{
    public class LoginResult
    {
        public User User { get; set; } = null!;
        public bool IsNewUser { get; set; }
    }
}
