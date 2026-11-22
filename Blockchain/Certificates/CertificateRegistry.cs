using System.Numerics;
using MathChain.Blockchain.Common;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace MathChain.Blockchain.Certificates
{
    public class CertificateRegistry : ICertificateRegistry
    {
        private const string NotConfiguredMessage = "Certificates are not configured on this server.";
        private const string NetworkUnavailableMessage = "The blockchain network could not be reached. Try again later.";

        private static readonly string UnauthorizedAccountSelector = SelectorHex("AccessControlUnauthorizedAccount(address,bytes32)");
        private static readonly string AlreadyIssuedSelector = SelectorHex("CertificateAlreadyIssued(uint256)");
        private static readonly string AlreadyRevokedSelector = SelectorHex("CertificateAlreadyRevoked(uint256)");
        private static readonly string NotFoundSelector = SelectorHex("CertificateNotFound(uint256)");
        private static readonly string NotAllowedToRevokeSelector = SelectorHex("NotAllowedToRevoke(address)");
        private static readonly string[] InsufficientFundsTexts = { "insufficient funds", "enough funds", "exceeds allowance" };

        private readonly BlockchainSettings _settings;
        private readonly ILogger<CertificateRegistry> _logger;
        private readonly Account? _issuer;
        private readonly Web3? _web3;
        private readonly SemaphoreSlim _sendLock;

        public CertificateRegistry(BlockchainSettings settings, PlatformTransactionLock transactionLock, ILogger<CertificateRegistry> logger)
        {
            _settings = settings;
            _sendLock = transactionLock.Gate;
            _logger = logger;

            CanRead = Uri.TryCreate(settings.RpcUrl, UriKind.Absolute, out _)
                && settings.ChainId > 0
                && AddressUtil.Current.IsValidEthereumAddressHexFormat(settings.CertificatesAddress ?? string.Empty);

            if (!CanRead)
            {
                return;
            }

            _issuer = CreateIssuer(settings);
            _web3 = _issuer == null ? new Web3(settings.RpcUrl) : new Web3(_issuer, settings.RpcUrl);
        }

        public bool CanRead { get; }

        public bool CanIssue => CanRead && _issuer != null;

        public CertificateNetworkInfo Network => new()
        {
            NetworkName = _settings.NetworkName,
            ChainId = _settings.ChainId,
            RpcUrl = _settings.RpcUrl,
            ExplorerUrl = _settings.ExplorerUrl.TrimEnd('/'),
            ContractAddress = _settings.CertificatesAddress,
            IssuerAddress = _issuer?.Address
        };

        public string CreateSalt()
        {
            return CertificateEncoding.CreateSalt();
        }

        public string ComputeDataHash(CertificateCommitment commitment)
        {
            return CertificateEncoding.ComputeDataHash(commitment);
        }

        public string BuildGrantIssuerRoleCallData()
        {
            EnsureCanIssue();

            return CertificateEncoding.BuildGrantIssuerRoleCallData(_issuer!.Address);
        }

        public async Task<CertificateBatchResult> IssueBatchAsync(CertificateKind kind, Guid subjectId, IReadOnlyList<CertificateIssueItem> items)
        {
            EnsureCanIssue();

            var message = new IssueBatchFunction
            {
                Students = items.Select(item => item.StudentAddress).ToList(),
                Kind = (byte)kind,
                SubjectId = CertificateEncoding.ToSubjectBytes(subjectId),
                DataHashes = items.Select(item => item.DataHash.HexToByteArray()).ToList()
            };

            var receipt = await ExecuteAsync(() => SendAsync(message));
            var tokens = receipt.DecodeAllEvents<CertificateIssuedEvent>()
                .Select(log => new IssuedCertificateToken
                {
                    StudentAddress = log.Event.Student.ToLowerInvariant(),
                    TokenId = (long)log.Event.TokenId
                })
                .ToList();

            if (tokens.Count != items.Count)
            {
                _logger.LogError("Transaction {TransactionHash} issued {Issued} certificates instead of {Expected}",
                    receipt.TransactionHash, tokens.Count, items.Count);
                throw new ServiceUnavailableException("The blockchain transaction did not issue every certificate.");
            }

            return new CertificateBatchResult { TransactionHash = receipt.TransactionHash, Tokens = tokens };
        }

        public async Task<string> RevokeAsync(long tokenId)
        {
            EnsureCanIssue();

            try
            {
                var receipt = await ExecuteAsync(() => SendAsync(new RevokeFunction { TokenId = new BigInteger(tokenId) }));

                return receipt.TransactionHash;
            }
            catch (CertificateNotActiveOnChainException)
            {
                return string.Empty;
            }
        }

        public async Task<OnChainCertificate?> GetAsync(long tokenId)
        {
            EnsureCanRead();

            return await ExecuteAsync(async () =>
            {
                var totalIssued = await _web3!.Eth.GetContractQueryHandler<TotalIssuedFunction>()
                    .QueryAsync<BigInteger>(_settings.CertificatesAddress, new TotalIssuedFunction());

                if (tokenId < 1 || tokenId > totalIssued)
                {
                    return null;
                }

                var output = await _web3.Eth.GetContractQueryHandler<GetCertificateFunction>()
                    .QueryDeserializingToObjectAsync<GetCertificateOutput>(
                        new GetCertificateFunction { TokenId = new BigInteger(tokenId) }, _settings.CertificatesAddress);
                var certificate = output.Certificate;

                return new OnChainCertificate
                {
                    TokenId = tokenId,
                    StudentAddress = certificate.Student.ToLowerInvariant(),
                    IssuerAddress = certificate.Issuer.ToLowerInvariant(),
                    Kind = (CertificateKind)certificate.Kind,
                    SubjectId = CertificateEncoding.ToSubjectGuid(certificate.SubjectId),
                    DataHash = CertificateEncoding.ToHex(certificate.DataHash),
                    IssuedAt = DateTimeOffset.FromUnixTimeSeconds((long)certificate.IssuedAt).UtcDateTime,
                    Revoked = certificate.Revoked
                };
            });
        }

        public async Task<long> FindActiveAsync(string studentAddress, CertificateKind kind, Guid subjectId)
        {
            EnsureCanRead();

            return await ExecuteAsync(async () =>
            {
                var tokenId = await _web3!.Eth.GetContractQueryHandler<FindCertificateFunction>()
                    .QueryAsync<BigInteger>(_settings.CertificatesAddress, new FindCertificateFunction
                    {
                        Student = studentAddress,
                        Kind = (byte)kind,
                        SubjectId = CertificateEncoding.ToSubjectBytes(subjectId)
                    });

                return (long)tokenId;
            });
        }

        public async Task<CertificateIssuerStatus> GetIssuerStatusAsync()
        {
            EnsureCanIssue();

            return await ExecuteAsync(async () =>
            {
                var hasRole = await _web3!.Eth.GetContractQueryHandler<HasRoleFunction>()
                    .QueryAsync<bool>(_settings.CertificatesAddress, new HasRoleFunction
                    {
                        Role = CertificateEncoding.IssuerRole,
                        Account = _issuer!.Address
                    });
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(_issuer.Address);

                return new CertificateIssuerStatus { HasIssuerRole = hasRole, Balance = Web3.Convert.FromWei(balance.Value) };
            });
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            EnsureCanRead();

            return await ExecuteAsync(async () =>
            {
                var balance = await _web3!.Eth.GetBalance.SendRequestAsync(address);

                return Web3.Convert.FromWei(balance.Value);
            });
        }

        private async Task<TransactionReceipt> SendAsync<TFunction>(TFunction message) where TFunction : FunctionMessage, new()
        {
            await _sendLock.WaitAsync();

            try
            {
                var handler = _web3!.Eth.GetContractTransactionHandler<TFunction>();
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();

                message.FromAddress = _issuer!.Address;
                message.GasPrice = gasPrice.Value * _settings.GasPriceMultiplierPercent / 100;

                var gasEstimate = await handler.EstimateGasAsync(_settings.CertificatesAddress, message);
                message.Gas = gasEstimate.Value * _settings.GasLimitMultiplierPercent / 100;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.ReceiptTimeoutSeconds));
                var receipt = await handler.SendRequestAndWaitForReceiptAsync(_settings.CertificatesAddress, message, timeout.Token);

                if (receipt.Status?.Value != BigInteger.One)
                {
                    _logger.LogError("Transaction {TransactionHash} was mined but failed", receipt.TransactionHash);
                    throw new ServiceUnavailableException("The blockchain transaction failed.");
                }

                return receipt;
            }
            finally
            {
                _sendLock.Release();
            }
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

                if (details.Contains(AlreadyRevokedSelector, StringComparison.OrdinalIgnoreCase)
                    || details.Contains(NotFoundSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new CertificateNotActiveOnChainException();
                }

                if (details.Contains(UnauthorizedAccountSelector, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("The issuer account {Issuer} does not have the issuer role", _issuer?.Address);
                    throw new ServiceUnavailableException(
                        "The platform account is not allowed to issue certificates yet. An administrator must grant it the issuer role.");
                }

                if (details.Contains(AlreadyIssuedSelector, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("The blockchain already holds an active certificate for one of these students.");
                }

                if (details.Contains(NotAllowedToRevokeSelector, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("The issuer account {Issuer} did not issue the certificate it tried to revoke", _issuer?.Address);
                    throw new ServiceUnavailableException("The platform account is not allowed to revoke this certificate.");
                }

                if (InsufficientFundsTexts.Any(text => details.Contains(text, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogError("The issuer account {Issuer} cannot pay for the transaction", _issuer?.Address);
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

                if (current is RpcResponseException rpcError && rpcError.RpcError?.Data != null)
                {
                    parts.Add(rpcError.RpcError.Data.ToString() ?? string.Empty);
                }
            }

            return string.Join(" ", parts);
        }

        private Account? CreateIssuer(BlockchainSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.IssuerPrivateKey))
            {
                return null;
            }

            var key = settings.IssuerPrivateKey.Trim();
            var hexKey = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key : "0x" + key;

            if (!CertificateEncoding.IsBytes32(hexKey))
            {
                _logger.LogError("Blockchain:IssuerPrivateKey is not a 32 byte hexadecimal value; certificates cannot be issued");
                return null;
            }

            return new Account(hexKey, settings.ChainId);
        }

        private void EnsureCanRead()
        {
            if (!CanRead)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }
        }

        private void EnsureCanIssue()
        {
            if (!CanIssue)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }
        }

        private static string SelectorHex(string signature)
        {
            return Convert.ToHexString(CertificateEncoding.ErrorSelector(signature)).ToLowerInvariant();
        }

        private sealed class CertificateNotActiveOnChainException : Exception
        {
        }
    }
}
