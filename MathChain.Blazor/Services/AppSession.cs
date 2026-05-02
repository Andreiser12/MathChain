using MathChain.Domain.Entities;

namespace MathChain.Blazor.Services
{
    public class AppSession
    {
        private string _walletAddress = string.Empty;
        public bool IsConnected { get; set; } = false;
        public HashSet<Guid> MarkedFormulas { get; set; } = new();

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
