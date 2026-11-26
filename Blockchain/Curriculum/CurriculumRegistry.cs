using System.Numerics;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Common;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace MathChain.Blockchain.Curriculum
{
    public class CurriculumRegistry : ICurriculumRegistry
    {
        private const string NotConfiguredMessage = "Curriculum anchoring is not configured on this server.";
        private const string NetworkUnavailableMessage = "The blockchain network could not be reached. Try again later.";
        private static readonly string UnauthorizedSelector = Selector("AccessControlUnauthorizedAccount(address,bytes32)");
        private static readonly string AlreadyPublishedSelector = Selector("AlreadyPublished(uint256)");
        private static readonly string InvalidIdentifierSelector = Selector("InvalidContentIdentifier()");
        private static readonly string[] InsufficientFundsTexts = { "insufficient funds", "enough funds", "exceeds allowance" };

        private readonly BlockchainSettings _settings;
        private readonly ILogger<CurriculumRegistry> _logger;
        private readonly SemaphoreSlim _sendLock;
        private readonly Account? _publisher;
        private readonly Web3? _web3;

        public CurriculumRegistry(BlockchainSettings settings, PlatformTransactionLock transactionLock, ILogger<CurriculumRegistry> logger)
        {
            _settings = settings;
            _logger = logger;
            _sendLock = transactionLock.Gate;

            CanRead = Uri.TryCreate(settings.RpcUrl, UriKind.Absolute, out _)
                && settings.ChainId > 0
                && AddressUtil.Current.IsValidEthereumAddressHexFormat(settings.CurriculumRegistryAddress ?? string.Empty);

            if (!CanRead)
            {
                return;
            }

            var key = settings.IssuerPrivateKey?.Trim() ?? string.Empty;
            var hexKey = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key : "0x" + key;
            _publisher = CertificateEncoding.IsBytes32(hexKey) ? new Account(hexKey, settings.ChainId) : null;
            _web3 = _publisher == null ? new Web3(settings.RpcUrl) : new Web3(_publisher, settings.RpcUrl);
        }

        public bool CanRead { get; }

        public bool CanPublish => CanRead && _publisher != null;

        public CurriculumRegistryInfo Info => new()
        {
            NetworkName = _settings.NetworkName,
            ChainId = _settings.ChainId,
            ExplorerUrl = _settings.ExplorerUrl.TrimEnd('/'),
            ContractAddress = _settings.CurriculumRegistryAddress,
            PublisherAddress = _publisher?.Address
        };

        public async Task<CurriculumAnchor?> GetLatestAsync()
        {
            if (!CanRead)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }

            return await ExecuteAsync(async () =>
            {
                var count = await _web3!.Eth.GetContractQueryHandler<PublicationCountFunction>()
                    .QueryAsync<BigInteger>(_settings.CurriculumRegistryAddress, new PublicationCountFunction());

                if (count == 0)
                {
                    return null;
                }

                var output = await _web3.Eth.GetContractQueryHandler<PublicationAtFunction>()
                    .QueryDeserializingToObjectAsync<PublicationOutput>(new PublicationAtFunction { Version = count }, _settings.CurriculumRegistryAddress);

                return new CurriculumAnchor
                {
                    Version = (long)count,
                    Cid = output.Publication.Cid,
                    PublishedAt = DateTimeOffset.FromUnixTimeSeconds((long)output.Publication.PublishedAt).UtcDateTime,
                    Publisher = output.Publication.Publisher.ToLowerInvariant()
                };
            });
        }

        public async Task<CurriculumAnchor> PublishAsync(byte[] cid)
        {
            if (!CanPublish)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }

            return await ExecuteAsync(async () =>
            {
                await _sendLock.WaitAsync();

                try
                {
                    var handler = _web3!.Eth.GetContractTransactionHandler<PublishCurriculumFunction>();
                    var message = new PublishCurriculumFunction { Cid = cid, FromAddress = _publisher!.Address };
                    message.GasPrice = (await _web3.Eth.GasPrice.SendRequestAsync()).Value * _settings.GasPriceMultiplierPercent / 100;
                    message.Gas = (await handler.EstimateGasAsync(_settings.CurriculumRegistryAddress, message)).Value * _settings.GasLimitMultiplierPercent / 100;

                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.ReceiptTimeoutSeconds));
                    var receipt = await handler.SendRequestAndWaitForReceiptAsync(_settings.CurriculumRegistryAddress, message, timeout.Token);

                    if (receipt.Status?.Value != BigInteger.One)
                    {
                        _logger.LogError("Curriculum publication {TransactionHash} was mined but failed", receipt.TransactionHash);
                        throw new ServiceUnavailableException("The blockchain transaction failed.");
                    }

                    var published = receipt.DecodeAllEvents<CurriculumPublishedEvent>()
                        .FirstOrDefault(e => e.Log.Address.Equals(_settings.CurriculumRegistryAddress, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ServiceUnavailableException("The blockchain transaction did not publish the curriculum.");

                    return new CurriculumAnchor
                    {
                        Version = (long)published.Event.Version,
                        Cid = published.Event.Cid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds((long)published.Event.PublishedAt).UtcDateTime,
                        Publisher = published.Event.Publisher.ToLowerInvariant(),
                        TransactionHash = receipt.TransactionHash
                    };
                }
                finally
                {
                    _sendLock.Release();
                }
            });
        }

        private async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var details = Describe(exception);

                if (details.Contains(AlreadyPublishedSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("This curriculum is already the latest published version.");
                }

                if (details.Contains(UnauthorizedSelector, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("The platform account {Publisher} does not have the publisher role", _publisher?.Address);
                    throw new ServiceUnavailableException(
                        "The platform account is not allowed to publish the curriculum. An administrator must grant it the publisher role.");
                }

                if (details.Contains(InvalidIdentifierSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("The curriculum content identifier is not valid.");
                }

                if (InsufficientFundsTexts.Any(text => details.Contains(text, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogError("The platform account {Publisher} cannot pay for the transaction", _publisher?.Address);
                    throw new ServiceUnavailableException("The platform account does not have enough funds to pay for the transaction.");
                }

                _logger.LogError(exception, "Curriculum registry request failed");
                throw new ServiceUnavailableException(NetworkUnavailableMessage);
            }
        }

        private static string Describe(Exception exception)
        {
            var parts = new List<string>();

            for (var current = exception; current != null; current = current.InnerException)
            {
                parts.Add(current.Message);

                if (current is SmartContractCustomErrorRevertException customError)
                {
                    parts.Add(customError.ExceptionEncodedData ?? string.Empty);
                }

                if (current is RpcResponseException rpcError && rpcError.RpcError?.Data != null)
                {
                    parts.Add(rpcError.RpcError.Data.ToString() ?? string.Empty);
                }
            }

            return string.Join(" ", parts);
        }

        private static string Selector(string signature)
        {
            return Convert.ToHexString(CertificateEncoding.ErrorSelector(signature)).ToLowerInvariant();
        }
    }
}
