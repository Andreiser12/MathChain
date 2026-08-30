using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Blazor.Services
{
    public class AppSession
    {
        private string _walletAddress = string.Empty;
        public bool IsConnected { get; set; } = false;
        public HashSet<Guid> MarkedFormulas { get; set; } = new();
        public UserRole Role { get; set; }

        public string WalletAddress
        {
            get => _walletAddress;
            set
            {
                _walletAddress = value;
                NotifyStateChanged();
            }
        }

        public event Action? OnChange;

        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }

        public Formula? CurrentFormula { get; set; }
    }
}
