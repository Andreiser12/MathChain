using System.Numerics;
using System.Text.RegularExpressions;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Common;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace MathChain.Blockchain.Accounts
{
    public class SmartAccountService : ISmartAccountService
    {
        public const long VerificationGasForNewAccount = 500_000;
        public const long VerificationGasForExistingAccount = 150_000;
        public const long PaymasterVerificationGas = 120_000;
        public const long PaymasterPostOpGas = 0;
        public const long PreVerificationGas = 60_000;
        public const long ExecuteOverheadGas = 40_000;
        public const long FallbackCallGas = 400_000;

        private const string NotConfiguredMessage = "Smart accounts are not configured on this server.";
        private const string NetworkUnavailableMessage = "The blockchain network could not be reached. Try again later.";
        private const string FeesTooHighMessage = "Network fees are above the paymaster limit right now. Try again later.";
        private const string OperationRejectedMessage = "The blockchain rejected the certificate claim. An administrator must check the smart account setup.";
        private static readonly TimeSpan VoucherLifetime = TimeSpan.FromHours(1);
        private static readonly TimeSpan SponsorshipLifetime = TimeSpan.FromMinutes(10);

        private static readonly string FailedOpSelector = Selector("FailedOp(uint256,string)");
        private static readonly string FailedOpWithRevertSelector = Selector("FailedOpWithRevert(uint256,string,bytes)");
        private static readonly string CostTooHighSelector = Selector("CostTooHigh(uint256,uint256)");
        private static readonly Regex EntryPointErrorCode = new(@"\bAA\d\d\b", RegexOptions.Compiled);
        private static readonly Dictionary<string, string> ClaimErrors = new()
        {
            [Selector("InvalidVoucher()")] = "The certificate voucher is not valid.",
            [Selector("VoucherExpired(uint256)")] = "The certificate voucher has expired. Try again.",
            [Selector("VoucherAlreadyUsed()")] = "This certificate voucher has already been used.",
            [Selector("CertificateAlreadyIssued(uint256)")] = "The blockchain already holds an active certificate for this chapter.",
            [Selector("AccessControlUnauthorizedAccount(address,bytes32)")] =
                "The certificate claims contract is not allowed to issue certificates yet. An administrator must grant it the issuer role."
        };

        private readonly BlockchainSettings _settings;
        private readonly SemaphoreSlim _sendLock;
        private readonly ICustodialSigner? _ownerSigner;
        private readonly ILogger<SmartAccountService> _logger;
        private readonly Account? _platform;
        private readonly EthECKey? _platformKey;
        private readonly Web3? _web3;

        public SmartAccountService(BlockchainSettings settings, PlatformTransactionLock transactionLock, ILogger<SmartAccountService> logger,
            ICustodialSigner? ownerSigner = null)
        {
            _settings = settings;
            _sendLock = transactionLock.Gate;
            _logger = logger;
            _ownerSigner = ownerSigner;

            var addresses = new[]
            {
                settings.EntryPointAddress, settings.AccountFactoryAddress, settings.PaymasterAddress,
                settings.CertificateClaimsAddress, settings.CertificatesAddress
            };
            var key = settings.IssuerPrivateKey?.Trim() ?? string.Empty;
            var hexKey = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key : "0x" + key;

            IsConfigured = ownerSigner != null
                && Uri.TryCreate(settings.RpcUrl, UriKind.Absolute, out _)
                && settings.ChainId > 0
                && addresses.All(address => AddressUtil.Current.IsValidEthereumAddressHexFormat(address ?? string.Empty))
                && CertificateEncoding.IsBytes32(hexKey);

            if (!IsConfigured)
            {
                return;
            }

            _platformKey = new EthECKey(hexKey);
            _platform = new Account(hexKey, settings.ChainId);
            _web3 = new Web3(_platform, settings.RpcUrl);
        }

        public bool IsConfigured { get; }

        public async Task<string> GetAccountAddressAsync(string ownerAddress)
        {
            EnsureConfigured();

            return await ExecuteAsync(async () =>
            {
                var address = await _web3!.Eth.GetContractQueryHandler<AccountAddressFunction>()
                    .QueryAsync<string>(_settings.AccountFactoryAddress, new AccountAddressFunction { Owner = ownerAddress, Salt = 0 });

                return address.ToLowerInvariant();
            });
        }

        public async Task<bool> IsDeployedAsync(string accountAddress)
        {
            EnsureConfigured();

            return await ExecuteAsync(async () =>
            {
                var code = await _web3!.Eth.GetCode.SendRequestAsync(accountAddress);

                return !string.IsNullOrEmpty(code) && code != "0x";
            });
        }

        public async Task<SmartAccountClaimResult> ClaimCertificateAsync(SmartAccountClaimRequest request)
        {
            EnsureConfigured();

            var account = await GetAccountAddressAsync(request.OwnerAddress);
            var deployed = await IsDeployedAsync(account);
            var now = DateTimeOffset.UtcNow;
            var subjectId = CertificateEncoding.ToSubjectBytes(request.SubjectId);
            var dataHash = request.DataHash.HexToByteArray();
            var deadline = now.Add(VoucherLifetime).ToUnixTimeSeconds();
            var voucherDigest = UserOperationEncoding.ComputeVoucherDigest(_settings.CertificateClaimsAddress, _settings.ChainId, account,
                request.Kind, subjectId, dataHash, deadline);
            var claim = new ClaimCertificateFunction
            {
                Kind = (byte)request.Kind,
                SubjectId = subjectId,
                DataHash = dataHash,
                Deadline = deadline,
                Signature = UserOperationEncoding.SignDigest(_platformKey!, voucherDigest)
            };
            var callData = new ExecuteFunction
            {
                Destination = _settings.CertificateClaimsAddress,
                Value = 0,
                Data = claim.GetCallData()
            }.GetCallData();

            return await ExecuteAsync(async () =>
            {
                await _sendLock.WaitAsync();

                try
                {
                    var nonce = await _web3!.Eth.GetContractQueryHandler<GetNonceFunction>()
                        .QueryAsync<BigInteger>(_settings.EntryPointAddress, new GetNonceFunction { Sender = account, Key = 0 });
                    var callGas = await EstimateClaimGasAsync(account, claim);
                    var verificationGas = deployed ? VerificationGasForExistingAccount : VerificationGasForNewAccount;
                    var gasPrice = await ChooseGasPriceAsync(verificationGas + callGas + PaymasterVerificationGas + PaymasterPostOpGas + PreVerificationGas);
                    var validUntil = now.Add(SponsorshipLifetime).ToUnixTimeSeconds();
                    var operation = new PackedUserOperation
                    {
                        Sender = account,
                        Nonce = nonce,
                        InitCode = deployed
                            ? Array.Empty<byte>()
                            : UserOperationEncoding.BuildInitCode(_settings.AccountFactoryAddress,
                                new CreateAccountFunction { Owner = request.OwnerAddress, Salt = 0 }.GetCallData()),
                        CallData = callData,
                        AccountGasLimits = UserOperationEncoding.PackUints(verificationGas, callGas),
                        PreVerificationGas = PreVerificationGas,
                        GasFees = UserOperationEncoding.PackUints(gasPrice, gasPrice),
                        PaymasterAndData = UserOperationEncoding.BuildPaymasterData(_settings.PaymasterAddress, PaymasterVerificationGas,
                            PaymasterPostOpGas, validUntil, 0, new byte[65])
                    };

                    var sponsorHash = UserOperationEncoding.ComputeSponsorHash(operation, _settings.PaymasterAddress, _settings.ChainId, validUntil, 0);
                    operation.PaymasterAndData = UserOperationEncoding.BuildPaymasterData(_settings.PaymasterAddress, PaymasterVerificationGas,
                        PaymasterPostOpGas, validUntil, 0, UserOperationEncoding.SignPersonal(_platformKey!, sponsorHash));

                    var userOpHash = UserOperationEncoding.ComputeUserOpHash(operation, _settings.EntryPointAddress, _settings.ChainId);
                    operation.Signature = _ownerSigner!.SignPersonalHash(request.UserId, userOpHash).HexToByteArray();

                    var handleOps = new HandleOpsFunction
                    {
                        Ops = new List<PackedUserOperation> { operation },
                        Beneficiary = _platform!.Address,
                        FromAddress = _platform.Address,
                        GasPrice = gasPrice
                    };
                    var handler = _web3.Eth.GetContractTransactionHandler<HandleOpsFunction>();
                    var estimate = await handler.EstimateGasAsync(_settings.EntryPointAddress, handleOps);
                    handleOps.Gas = estimate.Value * _settings.GasLimitMultiplierPercent / 100;

                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.ReceiptTimeoutSeconds));
                    var receipt = await handler.SendRequestAndWaitForReceiptAsync(_settings.EntryPointAddress, handleOps, timeout.Token);

                    return ReadClaimResult(receipt, userOpHash, account);
                }
                finally
                {
                    _sendLock.Release();
                }
            });
        }

        public async Task<string> RevokeClaimedCertificateAsync(long tokenId)
        {
            EnsureConfigured();

            return await ExecuteAsync(async () =>
            {
                await _sendLock.WaitAsync();

                try
                {
                    var handler = _web3!.Eth.GetContractTransactionHandler<RevokeClaimedFunction>();
                    var message = new RevokeClaimedFunction { TokenId = tokenId, FromAddress = _platform!.Address };
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.ReceiptTimeoutSeconds));
                    var receipt = await handler.SendRequestAndWaitForReceiptAsync(_settings.CertificateClaimsAddress, message, timeout.Token);

                    if (receipt.Status?.Value != BigInteger.One)
                    {
                        throw new ServiceUnavailableException("The blockchain transaction failed.");
                    }

                    return receipt.TransactionHash;
                }
                finally
                {
                    _sendLock.Release();
                }
            });
        }

        public async Task<SmartAccountSetup> GetSetupAsync()
        {
            var setup = new SmartAccountSetup
            {
                Configured = IsConfigured,
                EntryPointAddress = _settings.EntryPointAddress,
                FactoryAddress = _settings.AccountFactoryAddress,
                PaymasterAddress = _settings.PaymasterAddress,
                ClaimsAddress = _settings.CertificateClaimsAddress
            };

            if (!IsConfigured)
            {
                return setup;
            }

            setup.GrantClaimsIssuerRoleCallData = CertificateEncoding.ToHex(
                new GrantRoleFunction { Role = CertificateEncoding.IssuerRole, Account = _settings.CertificateClaimsAddress }.GetCallData());
            setup.PaymasterDepositCallData = CertificateEncoding.ToHex(new PaymasterDepositFunction().GetCallData());

            return await ExecuteAsync(async () =>
            {
                var deposit = await _web3!.Eth.GetContractQueryHandler<DepositBalanceFunction>()
                    .QueryAsync<BigInteger>(_settings.EntryPointAddress, new DepositBalanceFunction { Account = _settings.PaymasterAddress });
                setup.PaymasterDeposit = Web3.Convert.FromWei(deposit);
                setup.ClaimsCanIssue = await _web3.Eth.GetContractQueryHandler<HasRoleFunction>()
                    .QueryAsync<bool>(_settings.CertificatesAddress, new HasRoleFunction
                    {
                        Role = CertificateEncoding.IssuerRole,
                        Account = _settings.CertificateClaimsAddress
                    });

                return setup;
            });
        }

        private async Task<BigInteger> ChooseGasPriceAsync(long requiredGas)
        {
            var networkPrice = (await _web3!.Eth.GasPrice.SendRequestAsync()).Value;
            var maxCost = await _web3.Eth.GetContractQueryHandler<MaxCostPerOperationFunction>()
                .QueryAsync<BigInteger>(_settings.PaymasterAddress, new MaxCostPerOperationFunction());
            var affordable = maxCost / requiredGas;

            if (affordable < networkPrice)
            {
                _logger.LogWarning("Gas price {NetworkPrice} needs more than the paymaster limit of {MaxCost} wei for {RequiredGas} gas",
                    networkPrice, maxCost, requiredGas);
                throw new ServiceUnavailableException(FeesTooHighMessage);
            }

            return BigInteger.Min(networkPrice * _settings.GasPriceMultiplierPercent / 100, affordable);
        }

        private async Task<long> EstimateClaimGasAsync(string account, ClaimCertificateFunction claim)
        {
            var input = new CallInput(CertificateEncoding.ToHex(claim.GetCallData()), _settings.CertificateClaimsAddress) { From = account };
            var estimate = await _web3!.Eth.Transactions.EstimateGas.SendRequestAsync(input);

            return (long)(estimate.Value * 130 / 100) + ExecuteOverheadGas;
        }

        private SmartAccountClaimResult ReadClaimResult(TransactionReceipt receipt, byte[] userOpHash, string account)
        {
            if (receipt.Status?.Value != BigInteger.One)
            {
                _logger.LogError("handleOps transaction {TransactionHash} failed", receipt.TransactionHash);
                throw new ServiceUnavailableException("The blockchain transaction failed.");
            }

            var operationEvent = receipt.DecodeAllEvents<UserOperationEvent>()
                .FirstOrDefault(log => log.Log.Address.Equals(_settings.EntryPointAddress, StringComparison.OrdinalIgnoreCase)
                    && log.Event.UserOpHash.SequenceEqual(userOpHash));

            if (operationEvent == null)
            {
                _logger.LogError("Transaction {TransactionHash} has no event for the user operation", receipt.TransactionHash);
                throw new ServiceUnavailableException("The blockchain transaction did not run the operation.");
            }

            if (!operationEvent.Event.Success)
            {
                var reason = receipt.DecodeAllEvents<UserOperationRevertReasonEvent>()
                    .FirstOrDefault(log => log.Event.UserOpHash.SequenceEqual(userOpHash))?.Event.RevertReason ?? Array.Empty<byte>();
                var selector = reason.Length >= 4 ? Convert.ToHexString(reason, 0, 4).ToLowerInvariant() : string.Empty;

                _logger.LogError("User operation in {TransactionHash} reverted with {Reason}", receipt.TransactionHash, CertificateEncoding.ToHex(reason));

                if (ClaimErrors.TryGetValue(selector, out var message))
                {
                    throw new BusinessRuleException(message);
                }

                throw new ServiceUnavailableException("The certificate could not be claimed on the blockchain.");
            }

            var issued = receipt.DecodeAllEvents<CertificateIssuedEvent>()
                .FirstOrDefault(log => log.Log.Address.Equals(_settings.CertificatesAddress, StringComparison.OrdinalIgnoreCase)
                    && log.Event.Student.Equals(account, StringComparison.OrdinalIgnoreCase));

            if (issued == null)
            {
                _logger.LogError("Transaction {TransactionHash} did not issue a certificate", receipt.TransactionHash);
                throw new ServiceUnavailableException("The blockchain transaction did not issue the certificate.");
            }

            return new SmartAccountClaimResult
            {
                AccountAddress = account,
                TransactionHash = receipt.TransactionHash,
                TokenId = (long)issued.Event.TokenId
            };
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

                foreach (var (selector, message) in ClaimErrors)
                {
                    if (details.Contains(selector, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new BusinessRuleException(message);
                    }
                }

                if (details.Contains("AA31", StringComparison.Ordinal))
                {
                    _logger.LogError("The paymaster deposit is too low");
                    throw new ServiceUnavailableException("The platform cannot pay for the transaction right now. An administrator must top up the paymaster.");
                }

                if (details.Contains(CostTooHighSelector, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("The paymaster refused the operation cost: {Details}", details);
                    throw new ServiceUnavailableException(FeesTooHighMessage);
                }

                if (EntryPointErrorCode.IsMatch(details))
                {
                    _logger.LogError(exception, "The EntryPoint rejected the user operation: {Details}", details);
                    throw new ServiceUnavailableException(OperationRejectedMessage);
                }

                if (details.Contains("insufficient funds", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("The platform account cannot pay for the transaction");
                    throw new ServiceUnavailableException("The platform account does not have enough funds to pay for the transaction.");
                }

                _logger.LogError(exception, "Smart account request failed: {Details}", details);
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

            var text = string.Join(" ", parts);

            foreach (var selector in new[] { FailedOpSelector, FailedOpWithRevertSelector })
            {
                var start = text.IndexOf(selector, StringComparison.OrdinalIgnoreCase);

                if (start < 0)
                {
                    continue;
                }

                var end = start;

                while (end < text.Length && Uri.IsHexDigit(text[end]))
                {
                    end++;
                }

                try
                {
                    var data = text[start..end].HexToByteArray();
                    var decoded = new ParameterDecoder().DecodeDefaultData(data[4..].ToHex(true),
                        new Nethereum.ABI.Model.Parameter("uint256", "opIndex", 1),
                        new Nethereum.ABI.Model.Parameter("string", "reason", 2));
                    text += " " + decoded[1].Result;
                }
                catch (Exception)
                {
                }
            }

            return text;
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }
        }

        private static string Selector(string signature)
        {
            return Convert.ToHexString(CertificateEncoding.ErrorSelector(signature)).ToLowerInvariant();
        }
    }
}
