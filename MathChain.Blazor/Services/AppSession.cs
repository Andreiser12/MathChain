using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Microsoft.JSInterop;

namespace MathChain.Blazor.Services
{
    public class AppSession
    {
        private readonly IJSRuntime _js;
        private string _walletAddress = string.Empty;

        public AppSession(IJSRuntime js)
        {
            _js = js;
        }

        public bool IsConnected { get; set; } = false;
        public HashSet<Guid> MarkedFormulas { get; set; } = new();
        public UserRole Role { get; set; }
        public Dictionary<Guid, (DateTime StartDate, DateTime EndDate)> ClassRoomDates { get; set; } = new();

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

        public async Task RestoreSessionAsync()
        {
            if (IsConnected && !string.IsNullOrWhiteSpace(_walletAddress))
            {
                return;
            }

            try
            {
                var savedWallet = await _js.InvokeAsync<string?>("localStorage.getItem", "mathchain_wallet_address");
                var savedRole = await _js.InvokeAsync<string?>("localStorage.getItem", "mathchain_user_role");

                if (!string.IsNullOrWhiteSpace(savedWallet))
                {
                    _walletAddress = savedWallet;
                    IsConnected = true;

                    if (Enum.TryParse<UserRole>(savedRole, out var parsedRole))
                    {
                        Role = parsedRole;
                    }

                    NotifyStateChanged();
                }
            }
            catch { }
        }

        public async Task SetSessionAsync(string walletAddress, UserRole role)
        {
            _walletAddress = walletAddress;
            IsConnected = true;
            Role = role;
            NotifyStateChanged();

            try
            {
                await _js.InvokeVoidAsync("localStorage.setItem", "mathchain_wallet_address", walletAddress);
                await _js.InvokeVoidAsync("localStorage.setItem", "mathchain_user_role", role.ToString());
            }
            catch { }
        }

        public async Task LogoutAsync()
        {
            _walletAddress = string.Empty;
            IsConnected = false;
            Role = default;
            MarkedFormulas.Clear();
            CurrentFormula = null;
            NotifyStateChanged();

            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", "mathchain_wallet_address");
                await _js.InvokeVoidAsync("localStorage.removeItem", "mathchain_user_role");
            }
            catch { }
        }

        public void Logout()
        {
            _walletAddress = string.Empty;
            IsConnected = false;
            Role = default;
            MarkedFormulas.Clear();
            CurrentFormula = null;
            NotifyStateChanged();
        }
    }
}
