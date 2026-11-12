using System.Globalization;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Microsoft.JSInterop;

namespace MathChain.Blazor.Services
{
    public class AppSession
    {
        private const string WalletKey = "mathchain_wallet_address";
        private const string RoleKey = "mathchain_user_role";
        private const string TokenKey = "mathchain_token";
        private const string TokenExpiresAtKey = "mathchain_token_expires_at";

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
        public string? Token { get; private set; }
        public DateTimeOffset? TokenExpiresAt { get; private set; }

        public bool HasValidToken => !string.IsNullOrEmpty(Token) && TokenExpiresAt > DateTimeOffset.UtcNow;

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

        public async Task SetCurrentFormulaAsync(Formula? formula)
        {
            CurrentFormula = formula;
            try
            {
                if (formula != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(formula);
                    await _js.InvokeVoidAsync("localStorage.setItem", "mathchain_current_formula", json);
                }
                else
                {
                    await _js.InvokeVoidAsync("localStorage.removeItem", "mathchain_current_formula");
                }
            }
            catch { }
        }

        public async Task<Formula?> RestoreCurrentFormulaAsync()
        {
            if (CurrentFormula != null) return CurrentFormula;

            try
            {
                var json = await _js.InvokeAsync<string?>("localStorage.getItem", "mathchain_current_formula");
                if (!string.IsNullOrWhiteSpace(json))
                {
                    CurrentFormula = System.Text.Json.JsonSerializer.Deserialize<Formula>(json);
                    return CurrentFormula;
                }
            }
            catch { }
            return null;
        }

        public async Task RestoreSessionAsync()
        {
            if (IsConnected && !string.IsNullOrWhiteSpace(_walletAddress))
            {
                if (!HasValidToken)
                {
                    await LogoutAsync();
                }
                return;
            }

            try
            {
                await RemoveLegacyEntriesAsync();

                var savedWallet = await _js.InvokeAsync<string?>("sessionStorage.getItem", WalletKey);
                var savedRole = await _js.InvokeAsync<string?>("sessionStorage.getItem", RoleKey);
                var savedToken = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
                var savedExpiresAt = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenExpiresAtKey);

                var hasExpiry = DateTimeOffset.TryParse(savedExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt);

                if (string.IsNullOrWhiteSpace(savedWallet) || string.IsNullOrWhiteSpace(savedToken) || !hasExpiry || expiresAt <= DateTimeOffset.UtcNow)
                {
                    await ClearStoredSessionAsync();
                    return;
                }

                _walletAddress = savedWallet;
                Token = savedToken;
                TokenExpiresAt = expiresAt;
                IsConnected = true;

                if (Enum.TryParse<UserRole>(savedRole, out var parsedRole))
                {
                    Role = parsedRole;
                }

                NotifyStateChanged();
            }
            catch { }
        }

        public async Task SetSessionAsync(string walletAddress, UserRole role, string token, DateTimeOffset tokenExpiresAt)
        {
            _walletAddress = walletAddress;
            IsConnected = true;
            Role = role;
            Token = token;
            TokenExpiresAt = tokenExpiresAt;
            NotifyStateChanged();

            try
            {
                await _js.InvokeVoidAsync("sessionStorage.setItem", WalletKey, walletAddress);
                await _js.InvokeVoidAsync("sessionStorage.setItem", RoleKey, role.ToString());
                await _js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
                await _js.InvokeVoidAsync("sessionStorage.setItem", TokenExpiresAtKey, tokenExpiresAt.ToString("O", CultureInfo.InvariantCulture));
            }
            catch { }
        }

        public async Task LogoutAsync()
        {
            Logout();

            try
            {
                await ClearStoredSessionAsync();
                await RemoveLegacyEntriesAsync();
            }
            catch { }
        }

        public void Logout()
        {
            _walletAddress = string.Empty;
            IsConnected = false;
            Role = default;
            Token = null;
            TokenExpiresAt = null;
            MarkedFormulas.Clear();
            CurrentFormula = null;
            NotifyStateChanged();
        }

        private async Task ClearStoredSessionAsync()
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", WalletKey);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RoleKey);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", TokenExpiresAtKey);
        }

        private async Task RemoveLegacyEntriesAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", WalletKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);
        }
    }
}
