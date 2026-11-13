using System.Security.Claims;
using MathChain.Domain.Enums;
using MathChain.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace MathChain.Infrastructure.Authentication
{
    public class HttpContextCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public string? WalletAddress => IsAuthenticated ? Principal!.FindFirst(WalletClaimTypes.Wallet)?.Value : null;

        public UserRole? Role
        {
            get
            {
                var roleClaim = IsAuthenticated ? Principal!.FindFirst(WalletClaimTypes.Role)?.Value : null;

                return Enum.TryParse<UserRole>(roleClaim, ignoreCase: false, out var role) && Enum.IsDefined(role) && !int.TryParse(roleClaim, out _)
                    ? role
                    : null;
            }
        }
    }
}
