using MathChain.Domain.Enums;

namespace MathChain.Blazor.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string WalletAddress { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
