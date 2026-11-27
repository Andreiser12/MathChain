using System.Numerics;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Curriculum;
using MathChain.Blockchain.Solutions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;

namespace MathChain.Blockchain.Indexing
{
    public class ChainLogReader : IChainLogReader
    {
        private static readonly string[] RangeErrorTexts = { "range", "too many", "10000 results", "limit exceeded", "block limit", "payload too large", "413" };

        private readonly ILogger<ChainLogReader> _logger;
        private readonly Web3? _web3;
        private readonly Dictionary<string, List<string>> _eventsByContract = new(StringComparer.OrdinalIgnoreCase);
        private readonly string[] _addresses = Array.Empty<string>();
        private readonly string[] _topics = Array.Empty<string>();

        public ChainLogReader(BlockchainSettings settings, ILogger<ChainLogReader> logger)
        {
            _logger = logger;
            ChainId = settings.ChainId;

            Register(settings.CertificatesAddress, Topic<CertificateIssuedEvent>(), Topic<CertificateRevokedEvent>());
            Register(settings.CertificateClaimsAddress, Topic<CertificateClaimedEvent>());
            Register(settings.AccountFactoryAddress, Topic<AccountCreatedEvent>());
            Register(settings.SolutionsAddress, Topic<SolutionPurchasedEvent>(), Topic<PriceChangedEvent>());
            Register(settings.CurriculumRegistryAddress, Topic<CurriculumPublishedEvent>());

            IsConfigured = Uri.TryCreate(settings.RpcUrl, UriKind.Absolute, out _) && settings.ChainId > 0 && _eventsByContract.Count > 0;

            if (IsConfigured)
            {
                _web3 = new Web3(settings.RpcUrl);
                _addresses = _eventsByContract.Keys.ToArray();
                _topics = _eventsByContract.Values.SelectMany(t => t).Distinct().ToArray();
            }
        }

        public bool IsConfigured { get; }

        public long ChainId { get; }

        public async Task<long> GetHeadAsync(CancellationToken cancellationToken)
        {
            return (long)(await _web3!.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
        }

        public async Task<DateTime> GetBlockTimeAsync(long block, CancellationToken cancellationToken)
        {
            var header = await _web3!.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(new BlockParameter(new HexBigInteger(block)));

            return DateTimeOffset.FromUnixTimeSeconds((long)header.Timestamp.Value).UtcDateTime;
        }

        public async Task<IReadOnlyList<ChainLogEvent>> GetLogsAsync(long fromBlock, long toBlock, CancellationToken cancellationToken)
        {
            var filter = new NewFilterInput
            {
                Address = _addresses,
                FromBlock = new BlockParameter(new HexBigInteger(fromBlock)),
                ToBlock = new BlockParameter(new HexBigInteger(toBlock)),
                Topics = new object[] { _topics }
            };

            FilterLog[] logs;

            try
            {
                logs = await _web3!.Eth.Filters.GetLogs.SendRequestAsync(filter);
            }
            catch (Exception exception) when (IsRangeError(exception))
            {
                throw new BlockRangeTooLargeException(exception.GetBaseException().Message, exception);
            }

            return logs
                .Where(log => log.Removed != true)
                .OrderBy(log => (long)log.BlockNumber.Value)
                .ThenBy(log => (long)log.LogIndex.Value)
                .Select(Decode)
                .Where(e => e != null)
                .Select(e => e!)
                .ToList();
        }

        public ChainLogEvent? Decode(FilterLog log)
        {
            if (log.Topics == null || log.Topics.Length == 0 || !_eventsByContract.TryGetValue(log.Address, out var allowed))
            {
                return null;
            }

            var topic = log.Topics[0]?.ToString()?.ToLowerInvariant() ?? string.Empty;

            if (!allowed.Contains(topic))
            {
                return null;
            }

            try
            {
                ChainLogEvent? decoded = topic switch
                {
                    _ when topic == Topic<CertificateIssuedEvent>() => FromIssued(log.DecodeEvent<CertificateIssuedEvent>().Event),
                    _ when topic == Topic<CertificateRevokedEvent>() => FromRevoked(log.DecodeEvent<CertificateRevokedEvent>().Event),
                    _ when topic == Topic<CertificateClaimedEvent>() => FromClaimed(log.DecodeEvent<CertificateClaimedEvent>().Event),
                    _ when topic == Topic<AccountCreatedEvent>() => FromAccount(log.DecodeEvent<AccountCreatedEvent>().Event),
                    _ when topic == Topic<SolutionPurchasedEvent>() => FromPurchase(log.DecodeEvent<SolutionPurchasedEvent>().Event),
                    _ when topic == Topic<PriceChangedEvent>() => FromPrice(log.DecodeEvent<PriceChangedEvent>().Event),
                    _ when topic == Topic<CurriculumPublishedEvent>() => FromCurriculum(log.DecodeEvent<CurriculumPublishedEvent>().Event),
                    _ => null
                };

                return decoded == null ? null : decoded with
                {
                    BlockNumber = (long)log.BlockNumber.Value,
                    TransactionHash = log.TransactionHash.ToLowerInvariant(),
                    LogIndex = (int)log.LogIndex.Value,
                    Contract = log.Address.ToLowerInvariant()
                };
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Skipping log {Transaction}:{LogIndex} that could not be decoded", log.TransactionHash, log.LogIndex?.Value);
                return null;
            }
        }

        public static bool IsRangeError(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is HttpRequestException { StatusCode: System.Net.HttpStatusCode.RequestEntityTooLarge })
                {
                    return true;
                }

                if (current is RpcResponseException or RpcClientUnknownException or HttpRequestException
                    && RangeErrorTexts.Any(t => current.Message.Contains(t, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        public static string Topic<TEvent>() where TEvent : IEventDTO
        {
            return ABITypedRegistry.GetEvent<TEvent>().Sha3Signature.ToLowerInvariant().EnsureHexPrefix();
        }

        private void Register(string? address, params string[] topics)
        {
            if (!string.IsNullOrWhiteSpace(address) && AddressUtil.Current.IsValidEthereumAddressHexFormat(address))
            {
                _eventsByContract[address.ToLowerInvariant()] = topics.ToList();
            }
        }

        private static string Hex(byte[] bytes)
        {
            return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static decimal Amount(BigInteger value)
        {
            return value <= new BigInteger(decimal.MaxValue) ? (decimal)value : throw new OverflowException("The amount does not fit in a decimal.");
        }

        private static ChainLogEvent FromIssued(CertificateIssuedEvent e)
        {
            return new CertificateIssuedLog((long)e.TokenId, e.Student.ToLowerInvariant(), e.Issuer.ToLowerInvariant(), (CertificateKind)e.Kind, Hex(e.SubjectId), Hex(e.DataHash));
        }

        private static ChainLogEvent FromRevoked(CertificateRevokedEvent e)
        {
            return new CertificateRevokedLog((long)e.TokenId, e.RevokedBy.ToLowerInvariant());
        }

        private static ChainLogEvent FromClaimed(CertificateClaimedEvent e)
        {
            return new CertificateClaimedLog((long)e.TokenId, e.Student.ToLowerInvariant());
        }

        private static ChainLogEvent FromAccount(AccountCreatedEvent e)
        {
            return new AccountCreatedLog(e.Account.ToLowerInvariant(), e.Owner.ToLowerInvariant());
        }

        private static ChainLogEvent FromPurchase(SolutionPurchasedEvent e)
        {
            return new SolutionPurchasedLog(e.Account.ToLowerInvariant(), Hex(e.SolutionId), e.Payer.ToLowerInvariant(), e.Tier, Amount(e.Price));
        }

        private static ChainLogEvent FromPrice(PriceChangedEvent e)
        {
            return new PriceChangedLog(e.Tier, Amount(e.Price));
        }

        private static ChainLogEvent FromCurriculum(CurriculumPublishedEvent e)
        {
            return new CurriculumPublishedLog((long)e.Version, e.Cid, e.Publisher.ToLowerInvariant());
        }
    }
}
