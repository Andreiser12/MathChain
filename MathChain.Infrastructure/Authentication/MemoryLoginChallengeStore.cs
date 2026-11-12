using MathChain.Domain.Entities;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MathChain.Infrastructure.Authentication
{
    public class MemoryLoginChallengeStore : ILoginChallengeStore
    {
        private const string KeyPrefix = "login-challenge:";

        private readonly IMemoryCache _cache;
        private readonly TimeProvider _timeProvider;
        private readonly object _sync = new();

        public MemoryLoginChallengeStore(IMemoryCache cache, TimeProvider timeProvider)
        {
            _cache = cache;
            _timeProvider = timeProvider;
        }

        public void Save(LoginChallenge challenge)
        {
            var lifetime = challenge.ExpiresAt - _timeProvider.GetUtcNow();

            if (lifetime <= TimeSpan.Zero)
            {
                return;
            }

            lock (_sync)
            {
                _cache.Set(KeyPrefix + challenge.WalletAddress, challenge, lifetime);
            }
        }

        public LoginChallenge? Take(string walletAddress)
        {
            var key = KeyPrefix + walletAddress;

            lock (_sync)
            {
                if (!_cache.TryGetValue(key, out LoginChallenge? challenge))
                {
                    return null;
                }

                _cache.Remove(key);
                return challenge;
            }
        }
    }
}
