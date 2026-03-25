using WalletConnectSharp.Desktop;
using WalletConnectSharp.Core.Models;
using WalletConnectSharp.Core;

namespace MathChain.WPF.Services
{
    public static class WalletConnectHelper
    {
        private static WalletConnect _client;

        public static async Task<string> GenerateUriAsync()
        {
            var metadata = new ClientMeta
            {
                Name = "MathChain",
                Description = "Blockchain Math Education Platform",
                URL = "https://mathchain.app",
                Icons = new[] { "https://mathchain.app/icon.png" }
            };

            _client = new WalletConnect(metadata, bridgeUrl: "https://safe-walletconnect.gnosis.io/");

            await _client.Connect();

            return _client.URI;
        }

        public static async Task<string> WaitForConnectionAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            while (!cts.Token.IsCancellationRequested)
            {
                if (_client != null && _client.Connected &&
                    _client.Accounts != null && _client.Accounts.Length > 0)
                {
                    return _client.Accounts[0];
                }
                await Task.Delay(200, cts.Token);
            }
            throw new TimeoutException("Connection timeout — user did not scan QR.");
        }
    }
}