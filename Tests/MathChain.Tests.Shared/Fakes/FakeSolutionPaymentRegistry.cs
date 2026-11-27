using MathChain.Blockchain.Solutions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeSolutionPaymentRegistry : ISolutionPaymentRegistry
    {
        public const string ContractAddress = "0x5555555555555555555555555555555555555555";
        public const string TokenAddress = "0x036cbd53842c5426634e7929541ec2318f3dcf7e";
        public const string RelayerAddress = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8";
        public const string ValidSignature = "0xsigned";

        private int _transactionCounter;

        public bool CanRead { get; set; } = true;
        public bool CanRelayValue { get; set; } = true;
        public bool CanRelay => CanRead && CanRelayValue;
        public SolutionPrices Prices { get; set; } = new() { Easy = 1_000_000, Medium = 2_000_000, Hard = 3_000_000 };
        public Dictionary<string, long> Balances { get; } = new();
        public HashSet<string> PurchasedOnChain { get; } = new();
        public List<SolutionPaymentOrder> SubmittedOrders { get; } = new();
        public Dictionary<string, Queue<SolutionPaymentState>> OutcomeStates { get; } = new();
        public Exception? SubmitError { get; set; }
        public SolutionPaymentState NextOutcome { get; set; } = SolutionPaymentState.Confirmed;
        public int PendingPollsBeforeOutcome { get; set; }
        public int OutcomeQueries { get; private set; }
        public Func<SolutionPaymentOrder, SolutionPaymentOutcome, SolutionPaymentOutcome>? ChangeOutcome { get; set; }
        public bool PricesUnavailable { get; set; }

        public SolutionPaymentNetwork Network => new()
        {
            NetworkName = "Test Network",
            ChainId = 84532,
            RpcUrl = "http://127.0.0.1:8545",
            ExplorerUrl = "https://explorer.example",
            ContractAddress = ContractAddress,
            TokenAddress = TokenAddress,
            TokenName = "USDC",
            TokenVersion = "2",
            TokenSymbol = "USDC",
            TokenDecimals = 6,
            RelayerAddress = CanRelay ? RelayerAddress : null
        };

        public string CreateSalt()
        {
            return "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        public string ComputeSolutionId(Guid formulaId, int exerciseIndex)
        {
            return SolutionEncoding.ComputeSolutionId(formulaId, exerciseIndex);
        }

        public string ComputeNonce(string accountAddress, string solutionId, SolutionTier tier, string salt)
        {
            return SolutionEncoding.ComputeNonce(84532, ContractAddress, accountAddress, solutionId, tier, salt);
        }

        public bool IsSignedByPayer(SolutionPaymentOrder order)
        {
            return order.Signature == ValidSignature;
        }

        public string BuildSetPriceCallData(SolutionTier tier, long priceUnits)
        {
            return $"setPrice:{tier}:{priceUnits}";
        }

        public Task<SolutionPrices> GetPricesAsync()
        {
            if (PricesUnavailable)
            {
                throw new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");
            }

            return Task.FromResult(Prices);
        }

        public Task<long> GetTokenBalanceAsync(string address)
        {
            return Task.FromResult(Balances.TryGetValue(address, out var balance) ? balance : 0);
        }

        public Task<bool> IsPurchasedAsync(string accountAddress, string solutionId)
        {
            return Task.FromResult(PurchasedOnChain.Contains(Key(accountAddress, solutionId)));
        }

        public Task<string> SubmitAsync(SolutionPaymentOrder order)
        {
            if (SubmitError != null)
            {
                throw SubmitError;
            }

            SubmittedOrders.Add(order);
            var hash = "0x" + (++_transactionCounter).ToString("x64");
            OutcomeStates[hash] = new Queue<SolutionPaymentState>(
                Enumerable.Repeat(SolutionPaymentState.Pending, PendingPollsBeforeOutcome).Append(NextOutcome));

            if (NextOutcome == SolutionPaymentState.Confirmed)
            {
                PurchasedOnChain.Add(Key(order.AccountAddress, order.SolutionId));
            }

            return Task.FromResult(hash);
        }

        public Task<SolutionPaymentOutcome> GetOutcomeAsync(string transactionHash)
        {
            OutcomeQueries++;
            var queue = OutcomeStates[transactionHash];
            var state = queue.Count > 1 ? queue.Dequeue() : queue.Peek();
            var index = int.Parse(transactionHash[2..], System.Globalization.NumberStyles.HexNumber) - 1;
            var order = SubmittedOrders[index];

            var outcome = state != SolutionPaymentState.Confirmed
                ? new SolutionPaymentOutcome { State = state }
                : new SolutionPaymentOutcome
                {
                    State = state,
                    AccountAddress = order.AccountAddress,
                    SolutionId = order.SolutionId,
                    PayerAddress = order.PayerAddress,
                    Tier = order.Tier,
                    PriceUnits = order.PriceUnits
                };

            return Task.FromResult(ChangeOutcome?.Invoke(order, outcome) ?? outcome);
        }

        public Task<SolutionRelayerStatus> GetRelayerStatusAsync()
        {
            return Task.FromResult(new SolutionRelayerStatus { Address = RelayerAddress, Balance = 0.25m });
        }

        public void ConfirmLater(string transactionHash)
        {
            OutcomeStates[transactionHash] = new Queue<SolutionPaymentState>(new[] { SolutionPaymentState.Confirmed });
            var index = int.Parse(transactionHash[2..], System.Globalization.NumberStyles.HexNumber) - 1;
            PurchasedOnChain.Add(Key(SubmittedOrders[index].AccountAddress, SubmittedOrders[index].SolutionId));
        }

        public static string Key(string accountAddress, string solutionId)
        {
            return accountAddress.ToLowerInvariant() + "|" + solutionId.ToLowerInvariant();
        }
    }
}
