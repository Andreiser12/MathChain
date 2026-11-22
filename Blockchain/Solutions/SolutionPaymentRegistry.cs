using System.Numerics;
using System.Text;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Common;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace MathChain.Blockchain.Solutions
{
    public class SolutionPaymentRegistry : ISolutionPaymentRegistry
    {
        private const string NotConfiguredMessage = "Paid solutions are not configured on this server.";
        private const string NetworkUnavailableMessage = "The blockchain network could not be reached. Try again later.";
        private const string ErrorStringSelector = "08c379a0";

        private static readonly string AlreadyPurchasedSelector = SelectorHex("AlreadyPurchased(address,bytes32)");
        private static readonly string PriceMismatchSelector = SelectorHex("PriceMismatch(uint256,uint256)");
        private static readonly string InsufficientBalanceSelector = SelectorHex("ERC20InsufficientBalance(address,uint256,uint256)");
        private static readonly string[] InsufficientGasTexts = { "insufficient funds", "enough funds" };

        private readonly BlockchainSettings _settings;
        private readonly SemaphoreSlim _sendLock;
        private readonly ILogger<SolutionPaymentRegistry> _logger;
        private readonly Account? _relayer;
        private readonly Web3? _web3;
        private readonly string _domainSeparator = string.Empty;
        private bool _tokenDomainVerified;

        public SolutionPaymentRegistry(BlockchainSettings settings, PlatformTransactionLock transactionLock, ILogger<SolutionPaymentRegistry> logger)
        {
            _settings = settings;
            _sendLock = transactionLock.Gate;
            _logger = logger;

            CanRead = Uri.TryCreate(settings.RpcUrl, UriKind.Absolute, out _)
                && settings.ChainId > 0
                && settings.PaymentTokenDecimals is >= 0 and <= 18
                && AddressUtil.Current.IsValidEthereumAddressHexFormat(settings.SolutionsAddress ?? string.Empty)
                && AddressUtil.Current.IsValidEthereumAddressHexFormat(settings.PaymentTokenAddress ?? string.Empty);

            if (!CanRead)
            {
                return;
            }

            _domainSeparator = SolutionEncoding.ComputeDomainSeparator(settings.PaymentTokenName, settings.PaymentTokenVersion,
                settings.ChainId, settings.PaymentTokenAddress);
            _relayer = CreateRelayer(settings);
            _web3 = _relayer == null ? new Web3(settings.RpcUrl) : new Web3(_relayer, settings.RpcUrl);
        }

        public bool CanRead { get; }

        public bool CanRelay => CanRead && _relayer != null;

        public SolutionPaymentNetwork Network => new()
        {
            NetworkName = _settings.NetworkName,
            ChainId = _settings.ChainId,
            RpcUrl = _settings.RpcUrl,
            ExplorerUrl = _settings.ExplorerUrl.TrimEnd('/'),
            ContractAddress = _settings.SolutionsAddress,
            TokenAddress = _settings.PaymentTokenAddress,
            TokenName = _settings.PaymentTokenName,
            TokenVersion = _settings.PaymentTokenVersion,
            TokenSymbol = _settings.PaymentTokenSymbol,
            TokenDecimals = _settings.PaymentTokenDecimals,
            RelayerAddress = _relayer?.Address
        };

        public string CreateSalt()
        {
            return CertificateEncoding.CreateSalt();
        }

        public string ComputeSolutionId(Guid formulaId, int exerciseIndex)
        {
            return SolutionEncoding.ComputeSolutionId(formulaId, exerciseIndex);
        }

        public string ComputeNonce(string accountAddress, string solutionId, SolutionTier tier, string salt)
        {
            EnsureCanRead();

            return SolutionEncoding.ComputeNonce(_settings.ChainId, _settings.SolutionsAddress, accountAddress, solutionId, tier, salt);
        }

        public bool IsSignedByPayer(SolutionPaymentOrder order)
        {
            EnsureCanRead();

            var nonce = ComputeNonce(order.AccountAddress, order.SolutionId, order.Tier, order.Salt);
            var digest = SolutionEncoding.ComputeAuthorizationDigest(_domainSeparator, order.PayerAddress, _settings.SolutionsAddress,
                order.PriceUnits, 0, order.ValidBefore, nonce);
            var signer = SolutionEncoding.RecoverSigner(digest, order.Signature);

            return signer != null && signer.Equals(order.PayerAddress, StringComparison.OrdinalIgnoreCase);
        }

        public string BuildSetPriceCallData(SolutionTier tier, long priceUnits)
        {
            EnsureCanRead();

            if (priceUnits <= 0)
            {
                throw new BusinessRuleException("The price must be greater than zero.");
            }

            var message = new SetSolutionPriceFunction { Tier = (byte)tier, Price = new BigInteger(priceUnits) };

            return CertificateEncoding.ToHex(message.GetCallData());
        }

        public async Task<SolutionPrices> GetPricesAsync()
        {
            EnsureCanRead();

            return await ExecuteAsync(async () =>
            {
                var output = await _web3!.Eth.GetContractQueryHandler<SolutionPricesFunction>()
                    .QueryDeserializingToObjectAsync<SolutionPricesOutput>(new SolutionPricesFunction(), _settings.SolutionsAddress);

                return new SolutionPrices
                {
                    Easy = (long)output.Easy,
                    Medium = (long)output.Medium,
                    Hard = (long)output.Hard
                };
            });
        }

        public async Task<long> GetTokenBalanceAsync(string address)
        {
            EnsureCanRead();

            return await ExecuteAsync(async () =>
            {
                var balance = await _web3!.Eth.GetContractQueryHandler<TokenBalanceOfFunction>()
                    .QueryAsync<BigInteger>(_settings.PaymentTokenAddress, new TokenBalanceOfFunction { Account = address });

                return balance > long.MaxValue ? long.MaxValue : (long)balance;
            });
        }

        public async Task<bool> IsPurchasedAsync(string accountAddress, string solutionId)
        {
            EnsureCanRead();

            return await ExecuteAsync(() => _web3!.Eth.GetContractQueryHandler<IsSolutionPurchasedFunction>()
                .QueryAsync<bool>(_settings.SolutionsAddress, new IsSolutionPurchasedFunction
                {
                    Account = accountAddress,
                    SolutionId = SolutionEncoding.RequireBytes32(solutionId)
                }));
        }

        public async Task<string> SubmitAsync(SolutionPaymentOrder order)
        {
            EnsureCanRelay();

            if (!SolutionEncoding.TrySplitSignature(order.Signature, out var v, out var r, out var s))
            {
                throw new BusinessRuleException("The payment signature is not valid.");
            }

            await EnsureTokenDomainAsync();

            var message = new BuySolutionFunction
            {
                Account = order.AccountAddress,
                SolutionId = SolutionEncoding.RequireBytes32(order.SolutionId),
                Tier = (byte)order.Tier,
                Salt = SolutionEncoding.RequireBytes32(order.Salt),
                Authorization = new PaymentAuthorization
                {
                    Payer = order.PayerAddress,
                    Value = new BigInteger(order.PriceUnits),
                    ValidAfter = BigInteger.Zero,
                    ValidBefore = new BigInteger(order.ValidBefore),
                    V = v,
                    R = r,
                    S = s
                }
            };

            return await ExecuteAsync(async () =>
            {
                await _sendLock.WaitAsync();

                try
                {
                    var handler = _web3!.Eth.GetContractTransactionHandler<BuySolutionFunction>();
                    var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();

                    message.FromAddress = _relayer!.Address;
                    message.GasPrice = gasPrice.Value * _settings.GasPriceMultiplierPercent / 100;

                    var gasEstimate = await handler.EstimateGasAsync(_settings.SolutionsAddress, message);
                    message.Gas = gasEstimate.Value * _settings.GasLimitMultiplierPercent / 100;

                    return await handler.SendRequestAsync(_settings.SolutionsAddress, message);
                }
                finally
                {
                    _sendLock.Release();
                }
            });
        }

        public async Task<SolutionPaymentOutcome> GetOutcomeAsync(string transactionHash)
        {
            EnsureCanRead();

            return await ExecuteAsync(async () =>
            {
                var receipt = await _web3!.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

                if (receipt == null)
                {
                    return new SolutionPaymentOutcome { State = SolutionPaymentState.Pending };
                }

                if (receipt.Status?.Value != BigInteger.One)
                {
                    return new SolutionPaymentOutcome { State = SolutionPaymentState.Reverted };
                }

                var purchase = receipt.DecodeAllEvents<SolutionPurchasedEvent>()
                    .FirstOrDefault(log => log.Log.Address.Equals(_settings.SolutionsAddress, StringComparison.OrdinalIgnoreCase));

                if (purchase == null)
                {
                    _logger.LogError("Transaction {TransactionHash} succeeded without a SolutionPurchased event", transactionHash);
                    return new SolutionPaymentOutcome { State = SolutionPaymentState.Reverted };
                }

                return new SolutionPaymentOutcome
                {
                    State = SolutionPaymentState.Confirmed,
                    AccountAddress = purchase.Event.Account.ToLowerInvariant(),
                    SolutionId = CertificateEncoding.ToHex(purchase.Event.SolutionId),
                    PayerAddress = purchase.Event.Payer.ToLowerInvariant(),
                    Tier = (SolutionTier)purchase.Event.Tier,
                    PriceUnits = (long)purchase.Event.Price
                };
            });
        }

        public async Task<SolutionRelayerStatus> GetRelayerStatusAsync()
        {
            EnsureCanRelay();

            return await ExecuteAsync(async () =>
            {
                var balance = await _web3!.Eth.GetBalance.SendRequestAsync(_relayer!.Address);

                return new SolutionRelayerStatus { Address = _relayer.Address, Balance = Web3.Convert.FromWei(balance.Value) };
            });
        }

        private async Task EnsureTokenDomainAsync()
        {
            if (_tokenDomainVerified)
            {
                return;
            }

            var onChain = await ExecuteAsync(() => _web3!.Eth.GetContractQueryHandler<TokenDomainSeparatorFunction>()
                .QueryAsync<byte[]>(_settings.PaymentTokenAddress, new TokenDomainSeparatorFunction()));

            if (!CertificateEncoding.ToHex(onChain).Equals(_domainSeparator, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("The payment token domain does not match Blockchain:PaymentTokenName and Blockchain:PaymentTokenVersion");
                throw new ServiceUnavailableException("The payment token is not configured correctly on this server.");
            }

            _tokenDomainVerified = true;
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
                var details = DescribeFailure(exception);

                if (details.Contains(AlreadyPurchasedSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SolutionAlreadyPurchasedException();
                }

                if (details.Contains(PriceMismatchSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("The price of this solution has changed. Open the payment window again.");
                }

                if (details.Contains("invalid signature", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("The payment signature is not valid.");
                }

                if (details.Contains("authorization is expired", StringComparison.OrdinalIgnoreCase)
                    || details.Contains("authorization is not yet valid", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("The payment authorization has expired. Open the payment window again.");
                }

                if (details.Contains("authorization is used", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("This payment authorization has already been used.");
                }

                if (details.Contains("exceeds balance", StringComparison.OrdinalIgnoreCase)
                    || details.Contains(InsufficientBalanceSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException($"The paying wallet does not have enough {_settings.PaymentTokenSymbol}.");
                }

                if (details.Contains("blacklisted", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("The payment token does not accept transfers from this wallet.");
                }

                if (InsufficientGasTexts.Any(text => details.Contains(text, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogError("The relayer account {Relayer} cannot pay for the transaction", _relayer?.Address);
                    throw new ServiceUnavailableException("The platform account does not have enough funds to pay for the transaction.");
                }

                _logger.LogError(exception, "Blockchain request failed");
                throw new ServiceUnavailableException(NetworkUnavailableMessage);
            }
        }

        private static string DescribeFailure(Exception exception)
        {
            var parts = new List<string>();

            for (var current = exception; current != null; current = current.InnerException)
            {
                parts.Add(current.Message);

                if (current is SmartContractCustomErrorRevertException customError)
                {
                    parts.Add(customError.ExceptionEncodedData ?? string.Empty);
                }

                if (current is SmartContractRevertException revert)
                {
                    parts.Add(revert.RevertMessage ?? string.Empty);
                    parts.Add(revert.EncodedData ?? string.Empty);
                }

                if (current is RpcResponseException rpcError && rpcError.RpcError?.Data != null)
                {
                    var data = rpcError.RpcError.Data.ToString() ?? string.Empty;
                    parts.Add(data);
                    parts.Add(DecodeErrorString(data));
                }
            }

            return string.Join(" ", parts);
        }

        private static string DecodeErrorString(string data)
        {
            var start = data.IndexOf("0x" + ErrorStringSelector, StringComparison.OrdinalIgnoreCase);

            if (start < 0)
            {
                return string.Empty;
            }

            var end = start + 2;

            while (end < data.Length && Uri.IsHexDigit(data[end]))
            {
                end++;
            }

            try
            {
                var bytes = data[(start + 2)..end].HexToByteArray();

                return new FunctionCallDecoder().DecodeFunctionErrorMessage(CertificateEncoding.ToHex(bytes)) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private Account? CreateRelayer(BlockchainSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.IssuerPrivateKey))
            {
                return null;
            }

            var key = settings.IssuerPrivateKey.Trim();
            var hexKey = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key : "0x" + key;

            return CertificateEncoding.IsBytes32(hexKey) ? new Account(hexKey, settings.ChainId) : null;
        }

        private void EnsureCanRead()
        {
            if (!CanRead)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }
        }

        private void EnsureCanRelay()
        {
            if (!CanRelay)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }
        }

        private static string SelectorHex(string signature)
        {
            return Convert.ToHexString(CertificateEncoding.ErrorSelector(signature)).ToLowerInvariant();
        }
    }
}
