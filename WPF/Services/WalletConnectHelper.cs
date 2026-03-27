using System.ComponentModel;
using System.Security.Policy;
using WalletConnectSharp.Core;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

namespace MathChain.WPF.Services
{
    public static class WalletConnectHelper
    {
        private static WalletConnectSignClient? _client;

        private static ConnectedData? _connectionData;

        public static async Task<string> GenerateUriAsync()
        {
            string projectId = Environment.GetEnvironmentVariable("PROJECT_ID");

            var options = new SignClientOptions()
            {
                ProjectId = projectId,
                Metadata = new Metadata()
                {
                    Name = "MathChain",
                    Description = "Blockchain Math Education Platform",
                    Url = "https://mathchain.app",
                    Icons = new[] { "https://mathchain.app/icon.png" }
                }
            };

            _client = await WalletConnectSignClient.Init(options);

            var connectOptions = new ConnectOptions()
            {
                OptionalNamespaces = new Dictionary<string, ProposedNamespace>()
                {
                    {
                        "eip155", new ProposedNamespace()
                        {
                            Methods = new[] { "eth_sendTransaction", "personal_sign" },
                            Chains = new[] { "eip155:11155111" },
                            Events = new[] { "chainChanged", "accountsChanged" }
                        }
                    }
                }
                
            };

            _connectionData = await _client.Connect(connectOptions);

            return _connectionData.Uri;
        }

        public static async Task<string> WaitForConnectionAsync()
        {
            if(_connectionData == null)
            {
                throw new Exception("QR Code must be generated first!");
            }

            var approvalTask = _connectionData.Approval;

            if(await Task.WhenAny(approvalTask, Task.Delay(TimeSpan.FromMinutes(2)))==approvalTask)
            {
                var session = await approvalTask;

                var account = session.Namespaces.ContainsKey("eip155") ? session.Namespaces["eip155"].Accounts.FirstOrDefault() : null;

                if (account != null)
                {
                    var parts = account.Split(':');
                    if (parts.Length >= 3)
                    {
                        return parts[2];
                    }
                    return account;
                }

                throw new Exception("Could not find the address of the wallet!");
            }
            else
            {
                throw new TimeoutException("Connection has expired!");
            }
        }
    }
}