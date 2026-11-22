using System.Globalization;
using System.Text.Json;
using MathChain.Application.DTOs;
using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;

namespace MathChain.Application.Contexts
{
    public class SolutionContext : ISolutionContext
    {
        public const string PendingStatus = "Pending";
        public const string CompletedStatus = "Completed";
        public const string FailedStatus = "Failed";

        private const string FormulaNotFoundMessage = "Formula not found.";
        private const string ExerciseNotFoundMessage = "Exercise not found.";
        private const string PurchaseNotFoundMessage = "Payment not found.";
        private const string NoSolutionMessage = "This exercise does not have a step-by-step solution yet.";
        private const string TransactionFailedMessage = "The payment transaction failed. No money was taken. Try again.";
        private const string MismatchedTransactionMessage = "The transaction does not match this payment. Contact the administrator.";

        private readonly ICurriculumGateway _curriculumGateway;
        private readonly IPracticeExerciseGateway _exerciseGateway;
        private readonly ISolutionPurchaseGateway _purchaseGateway;
        private readonly ISolutionPaymentRegistry _registry;
        private readonly ISolutionCatalog _catalog;
        private readonly AccessGuard _access;
        private readonly TimeProvider _timeProvider;
        private readonly SolutionPaymentOptions _options;

        public SolutionContext(ICurriculumGateway curriculumGateway, IPracticeExerciseGateway exerciseGateway,
            ISolutionPurchaseGateway purchaseGateway, ISolutionPaymentRegistry registry, ISolutionCatalog catalog,
            AccessGuard access, TimeProvider timeProvider, SolutionPaymentOptions options)
        {
            _curriculumGateway = curriculumGateway;
            _exerciseGateway = exerciseGateway;
            _purchaseGateway = purchaseGateway;
            _registry = registry;
            _catalog = catalog;
            _access = access;
            _timeProvider = timeProvider;
            _options = options;
        }

        public async Task<SolutionOfferDto> GetOfferAsync(Guid formulaId)
        {
            var wallet = _access.RequireWallet();
            var formula = await RequireFormulaAsync(formulaId);
            var exercises = ExerciseCatalog.ForFormula(formula.ExercisesJson);
            var freeAccess = HasFreeAccess();
            var purchased = (await _purchaseGateway.GetPurchasedIndicesAsync(wallet, formulaId)).ToHashSet();
            var prices = freeAccess ? null : await TryGetPricesAsync();

            return new SolutionOfferDto
            {
                FormulaId = formulaId,
                FreeAccess = freeAccess,
                CanPurchase = !freeAccess && prices != null && _registry.CanRelay,
                TokenSymbol = _registry.CanRead ? _registry.Network.TokenSymbol : string.Empty,
                Exercises = exercises.Exercises
                    .Select(definition =>
                    {
                        var tier = ParseTier(definition.Difficulty);

                        return new SolutionOfferItemDto
                        {
                            Index = definition.Index,
                            Difficulty = definition.Difficulty,
                            HasSolution = !exercises.UsesGenericExercises && _catalog.Find(formula.FormulaCode, definition.Template) != null,
                            Purchased = purchased.Contains(definition.Index),
                            Price = prices != null && tier != null ? ToTokenAmount(prices.For(tier.Value)) : null
                        };
                    })
                    .ToList()
            };
        }

        public async Task<SolutionQuoteDto> CreateQuoteAsync(Guid formulaId, int index, CreateSolutionQuoteRequest request)
        {
            var wallet = _access.RequireWallet();

            if (HasFreeAccess())
            {
                throw new BusinessRuleException("Your account can open every solution without paying.");
            }

            var payer = NormalizeAddress(request?.PayerAddress);
            var formula = await RequireFormulaAsync(formulaId);
            var definition = RequireSolvableDefinition(formula, index);
            var tier = ParseTier(definition.Difficulty)
                ?? throw new BusinessRuleException("This exercise does not have a price.");

            if (!_registry.CanRelay)
            {
                throw new ServiceUnavailableException("Paid solutions are not available right now.");
            }

            if ((await _purchaseGateway.GetPurchasedIndicesAsync(wallet, formulaId)).Contains(index))
            {
                throw new BusinessRuleException("You already own this solution.");
            }

            if (await _purchaseGateway.HasInProgressAsync(wallet, formulaId, index))
            {
                throw new BusinessRuleException("A payment for this solution is already being processed. Wait a moment and refresh the page.");
            }

            var solutionId = _registry.ComputeSolutionId(formulaId, index);
            var now = _timeProvider.GetUtcNow();

            if (await _registry.IsPurchasedAsync(wallet, solutionId))
            {
                await _purchaseGateway.AddAsync(new SolutionPurchase
                {
                    Id = Guid.NewGuid(),
                    AccountWallet = wallet,
                    FormulaId = formulaId,
                    ExerciseIndex = index,
                    Tier = tier,
                    PayerAddress = payer,
                    SolutionId = solutionId,
                    Status = SolutionPurchaseStatus.Completed,
                    CreatedAt = now.UtcDateTime,
                    CompletedAt = now.UtcDateTime
                });

                throw new BusinessRuleException("You already own this solution.");
            }

            var price = (await _registry.GetPricesAsync()).For(tier);
            var salt = _registry.CreateSalt();
            var validBefore = now.Add(_options.QuoteLifetime);
            var purchase = new SolutionPurchase
            {
                Id = Guid.NewGuid(),
                AccountWallet = wallet,
                FormulaId = formulaId,
                ExerciseIndex = index,
                Tier = tier,
                PriceUnits = price,
                PayerAddress = payer,
                SolutionId = solutionId,
                Salt = salt,
                Nonce = _registry.ComputeNonce(wallet, solutionId, tier, salt),
                ValidBefore = DateTimeOffset.FromUnixTimeSeconds(validBefore.ToUnixTimeSeconds()).UtcDateTime,
                Status = SolutionPurchaseStatus.Quoted,
                CreatedAt = now.UtcDateTime
            };

            var balance = await _registry.GetTokenBalanceAsync(payer);
            await _purchaseGateway.AddAsync(purchase);

            var network = _registry.Network;

            return new SolutionQuoteDto
            {
                PurchaseId = purchase.Id,
                Index = index,
                Difficulty = definition.Difficulty,
                Price = ToTokenAmount(price),
                TokenSymbol = network.TokenSymbol,
                TokenAddress = network.TokenAddress,
                PayerBalance = ToTokenAmount(balance),
                PayerAddress = payer,
                ExpiresAt = purchase.ValidBefore,
                NetworkName = network.NetworkName,
                ChainId = network.ChainId,
                RpcUrl = network.RpcUrl,
                ExplorerUrl = network.ExplorerUrl,
                TypedData = BuildTypedData(network, purchase)
            };
        }

        public async Task<SolutionPurchaseDto> SubmitPaymentAsync(Guid purchaseId, SubmitSolutionPaymentRequest request)
        {
            var purchase = await RequireOwnPurchaseAsync(purchaseId);

            switch (purchase.Status)
            {
                case SolutionPurchaseStatus.Completed:
                    return ToDto(purchase, CompletedStatus, null);
                case SolutionPurchaseStatus.Failed:
                    throw new BusinessRuleException(purchase.FailureReason ?? TransactionFailedMessage);
                case SolutionPurchaseStatus.Submitting:
                case SolutionPurchaseStatus.Submitted:
                    return await RefreshAsync(purchase);
            }

            if (_timeProvider.GetUtcNow().UtcDateTime >= purchase.ValidBefore)
            {
                await _purchaseGateway.MarkFailedAsync(purchase.Id, "The payment window has expired.");
                throw new BusinessRuleException("The payment window has expired. Open it again.");
            }

            var order = new SolutionPaymentOrder
            {
                AccountAddress = purchase.AccountWallet,
                SolutionId = purchase.SolutionId,
                Tier = purchase.Tier,
                Salt = purchase.Salt,
                PayerAddress = purchase.PayerAddress,
                PriceUnits = purchase.PriceUnits,
                ValidBefore = new DateTimeOffset(purchase.ValidBefore, TimeSpan.Zero).ToUnixTimeSeconds(),
                Signature = request?.Signature?.Trim() ?? string.Empty
            };

            if (!_registry.IsSignedByPayer(order))
            {
                throw new BusinessRuleException("The signature does not match the paying wallet. Sign the payment again.");
            }

            if (!await _purchaseGateway.TryStartSubmittingAsync(purchase.Id))
            {
                return await RefreshAsync(await RequireOwnPurchaseAsync(purchaseId));
            }

            string transactionHash;

            try
            {
                transactionHash = await _registry.SubmitAsync(order);
            }
            catch (SolutionAlreadyPurchasedException)
            {
                return await CompleteIfPaidOnChainAsync(purchase);
            }
            catch (BusinessRuleException exception)
            {
                await _purchaseGateway.MarkFailedAsync(purchase.Id, exception.Message);
                throw;
            }
            catch
            {
                await _purchaseGateway.ReturnToQuotedAsync(purchase.Id);
                throw;
            }

            await _purchaseGateway.MarkSubmittedAsync(purchase.Id, transactionHash);
            purchase.Status = SolutionPurchaseStatus.Submitted;
            purchase.TransactionHash = transactionHash;

            return await WaitForConfirmationAsync(purchase);
        }

        public async Task<SolutionPurchaseDto> GetPurchaseAsync(Guid purchaseId)
        {
            var purchase = await RequireOwnPurchaseAsync(purchaseId);

            return purchase.Status switch
            {
                SolutionPurchaseStatus.Completed => ToDto(purchase, CompletedStatus, null),
                SolutionPurchaseStatus.Failed => ToDto(purchase, FailedStatus, purchase.FailureReason),
                SolutionPurchaseStatus.Submitted => await RefreshAsync(purchase),
                _ => ToDto(purchase, PendingStatus, null)
            };
        }

        public async Task<ExerciseSolutionDto> GetSolutionAsync(Guid exerciseId)
        {
            var wallet = _access.RequireWallet();
            var exercise = await _exerciseGateway.GetByIdAsync(exerciseId);

            if (exercise == null || !exercise.StudentWallet.Equals(wallet, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotFoundException(ExerciseNotFoundMessage);
            }

            var formula = await RequireFormulaAsync(exercise.FormulaId);
            var definition = RequireSolvableDefinition(formula, exercise.ExerciseIndex);
            var solution = _catalog.Find(formula.FormulaCode, definition.Template)!;

            if (!HasFreeAccess()
                && !(await _purchaseGateway.GetPurchasedIndicesAsync(wallet, exercise.FormulaId)).Contains(exercise.ExerciseIndex))
            {
                throw new ForbiddenException("Pay for this solution to see it.");
            }

            if (!SolutionRenderer.TryReadValues(exercise.VariableValues, out var values))
            {
                throw new BusinessRuleException("Load the exercise again to see its solution.");
            }

            return new ExerciseSolutionDto
            {
                ExerciseId = exercise.Id,
                Steps = SolutionRenderer.Render(solution, values)
                    .Select(step => new SolutionStepDto { Text = step.Text, Latex = step.Latex })
                    .ToList(),
                Answer = exercise.ExpectedAnswer,
                Form = exercise.Form
            };
        }

        public async Task<SolutionPaymentSetupDto> GetPaymentSetupAsync()
        {
            _access.RequireRole(UserRole.Admin);

            var setup = new SolutionPaymentSetupDto { CanRead = _registry.CanRead, CanRelay = _registry.CanRelay };

            if (!_registry.CanRead)
            {
                return setup;
            }

            var network = _registry.Network;
            setup.NetworkName = network.NetworkName;
            setup.ChainId = network.ChainId;
            setup.RpcUrl = network.RpcUrl;
            setup.ExplorerUrl = network.ExplorerUrl;
            setup.ContractAddress = network.ContractAddress;
            setup.TokenAddress = network.TokenAddress;
            setup.TokenSymbol = network.TokenSymbol;

            var prices = await _registry.GetPricesAsync();
            setup.EasyPrice = ToTokenAmount(prices.Easy);
            setup.MediumPrice = ToTokenAmount(prices.Medium);
            setup.HardPrice = ToTokenAmount(prices.Hard);

            if (_registry.CanRelay)
            {
                var relayer = await _registry.GetRelayerStatusAsync();
                setup.RelayerAddress = relayer.Address;
                setup.RelayerBalance = relayer.Balance;
            }

            return setup;
        }

        public SolutionPriceChangeDto BuildPriceChange(ChangeSolutionPriceRequest request)
        {
            _access.RequireRole(UserRole.Admin);

            var tier = ParseTier(request?.Difficulty)
                ?? throw new BusinessRuleException("Difficulty must be Easy, Medium or Hard.");

            if (!_registry.CanRead)
            {
                throw new ServiceUnavailableException("Paid solutions are not configured on this server.");
            }

            var price = request!.Price;

            if (price <= 0 || price > _options.MaximumPrice)
            {
                throw new BusinessRuleException($"The price must be greater than 0 and at most {_options.MaximumPrice.ToString(CultureInfo.InvariantCulture)}.");
            }

            var scaled = price * TokenScale();

            if (scaled != decimal.Truncate(scaled))
            {
                throw new BusinessRuleException($"The price can have at most {_registry.Network.TokenDecimals} decimals.");
            }

            var units = (long)scaled;

            return new SolutionPriceChangeDto
            {
                To = _registry.Network.ContractAddress,
                Data = _registry.BuildSetPriceCallData(tier, units),
                PriceUnits = units
            };
        }

        private async Task<SolutionPurchaseDto> WaitForConfirmationAsync(SolutionPurchase purchase)
        {
            var deadline = _timeProvider.GetUtcNow().Add(_options.ConfirmationWait);

            while (true)
            {
                var result = await RefreshAsync(purchase);

                if (result.Status != PendingStatus || _timeProvider.GetUtcNow() >= deadline)
                {
                    return result;
                }

                await Task.Delay(_options.PollInterval);
            }
        }

        private async Task<SolutionPurchaseDto> RefreshAsync(SolutionPurchase purchase)
        {
            if (purchase.Status == SolutionPurchaseStatus.Completed)
            {
                return ToDto(purchase, CompletedStatus, null);
            }

            if (purchase.Status == SolutionPurchaseStatus.Failed)
            {
                return ToDto(purchase, FailedStatus, purchase.FailureReason);
            }

            if (purchase.Status != SolutionPurchaseStatus.Submitted || string.IsNullOrEmpty(purchase.TransactionHash))
            {
                return ToDto(purchase, PendingStatus, null);
            }

            var outcome = await _registry.GetOutcomeAsync(purchase.TransactionHash);

            switch (outcome.State)
            {
                case SolutionPaymentState.Pending:
                    return ToDto(purchase, PendingStatus, null);
                case SolutionPaymentState.Confirmed when Matches(purchase, outcome):
                    return await CompleteAsync(purchase);
                case SolutionPaymentState.Confirmed:
                    await _purchaseGateway.MarkFailedAsync(purchase.Id, MismatchedTransactionMessage);
                    return ToDto(purchase, FailedStatus, MismatchedTransactionMessage);
                default:
                    return await CompleteIfPaidOnChainAsync(purchase);
            }
        }

        private async Task<SolutionPurchaseDto> CompleteIfPaidOnChainAsync(SolutionPurchase purchase)
        {
            if (await _registry.IsPurchasedAsync(purchase.AccountWallet, purchase.SolutionId))
            {
                return await CompleteAsync(purchase);
            }

            await _purchaseGateway.MarkFailedAsync(purchase.Id, TransactionFailedMessage);

            return ToDto(purchase, FailedStatus, TransactionFailedMessage);
        }

        private async Task<SolutionPurchaseDto> CompleteAsync(SolutionPurchase purchase)
        {
            await _purchaseGateway.TryCompleteAsync(purchase.Id, _timeProvider.GetUtcNow().UtcDateTime);

            return ToDto(purchase, CompletedStatus, null);
        }

        private static bool Matches(SolutionPurchase purchase, SolutionPaymentOutcome outcome)
        {
            return string.Equals(outcome.AccountAddress, purchase.AccountWallet, StringComparison.OrdinalIgnoreCase)
                && string.Equals(outcome.SolutionId, purchase.SolutionId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(outcome.PayerAddress, purchase.PayerAddress, StringComparison.OrdinalIgnoreCase)
                && outcome.Tier == purchase.Tier
                && outcome.PriceUnits == purchase.PriceUnits;
        }

        private SolutionPurchaseDto ToDto(SolutionPurchase purchase, string status, string? message)
        {
            var explorer = _registry.CanRead ? _registry.Network.ExplorerUrl : string.Empty;

            return new SolutionPurchaseDto
            {
                PurchaseId = purchase.Id,
                FormulaId = purchase.FormulaId,
                Index = purchase.ExerciseIndex,
                Status = status,
                TransactionHash = purchase.TransactionHash,
                TransactionUrl = string.IsNullOrEmpty(purchase.TransactionHash) || string.IsNullOrEmpty(explorer)
                    ? null
                    : $"{explorer}/tx/{purchase.TransactionHash}",
                Message = message
            };
        }

        private async Task<SolutionPurchase> RequireOwnPurchaseAsync(Guid purchaseId)
        {
            var wallet = _access.RequireWallet();
            var purchase = await _purchaseGateway.GetByIdAsync(purchaseId);

            if (purchase == null || !purchase.AccountWallet.Equals(wallet, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotFoundException(PurchaseNotFoundMessage);
            }

            return purchase;
        }

        private async Task<CurriculumFormula> RequireFormulaAsync(Guid formulaId)
        {
            return await _curriculumGateway.GetFormulaByIdAsync(formulaId) ?? throw new NotFoundException(FormulaNotFoundMessage);
        }

        private PracticeExerciseDefinition RequireSolvableDefinition(CurriculumFormula formula, int index)
        {
            var exercises = ExerciseCatalog.ForFormula(formula.ExercisesJson);

            if (exercises.UsesGenericExercises || index < 0 || index >= exercises.Exercises.Count)
            {
                throw new NotFoundException(NoSolutionMessage);
            }

            var definition = exercises.Exercises[index];

            if (_catalog.Find(formula.FormulaCode, definition.Template) == null)
            {
                throw new NotFoundException(NoSolutionMessage);
            }

            return definition;
        }

        private async Task<SolutionPrices?> TryGetPricesAsync()
        {
            if (!_registry.CanRead)
            {
                return null;
            }

            try
            {
                return await _registry.GetPricesAsync();
            }
            catch (ServiceUnavailableException)
            {
                return null;
            }
        }

        private bool HasFreeAccess()
        {
            return _access.IsAdmin || _access.IsInRole(UserRole.Teacher);
        }

        private string NormalizeAddress(string? address)
        {
            var value = address?.Trim().ToLowerInvariant() ?? string.Empty;

            if (value.Length != 42 || !value.StartsWith("0x") || !value[2..].All(Uri.IsHexDigit))
            {
                throw new BusinessRuleException("Connect a wallet to pay for the solution.");
            }

            return value;
        }

        private decimal TokenScale()
        {
            var decimals = _registry.CanRead ? _registry.Network.TokenDecimals : 0;
            var scale = 1m;

            for (var i = 0; i < decimals; i++)
            {
                scale *= 10;
            }

            return scale;
        }

        private decimal ToTokenAmount(long units)
        {
            return units / TokenScale();
        }

        private static SolutionTier? ParseTier(string? difficulty)
        {
            return Enum.TryParse<SolutionTier>(difficulty?.Trim(), true, out var tier) && Enum.IsDefined(tier) ? tier : null;
        }

        private static string BuildTypedData(SolutionPaymentNetwork network, SolutionPurchase purchase)
        {
            var typedData = new
            {
                types = new
                {
                    EIP712Domain = new[]
                    {
                        new { name = "name", type = "string" },
                        new { name = "version", type = "string" },
                        new { name = "chainId", type = "uint256" },
                        new { name = "verifyingContract", type = "address" }
                    },
                    ReceiveWithAuthorization = new[]
                    {
                        new { name = "from", type = "address" },
                        new { name = "to", type = "address" },
                        new { name = "value", type = "uint256" },
                        new { name = "validAfter", type = "uint256" },
                        new { name = "validBefore", type = "uint256" },
                        new { name = "nonce", type = "bytes32" }
                    }
                },
                primaryType = "ReceiveWithAuthorization",
                domain = new
                {
                    name = network.TokenName,
                    version = network.TokenVersion,
                    chainId = network.ChainId,
                    verifyingContract = network.TokenAddress
                },
                message = new
                {
                    from = purchase.PayerAddress,
                    to = network.ContractAddress,
                    value = purchase.PriceUnits.ToString(CultureInfo.InvariantCulture),
                    validAfter = "0",
                    validBefore = new DateTimeOffset(purchase.ValidBefore, TimeSpan.Zero).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    nonce = purchase.Nonce
                }
            };

            return JsonSerializer.Serialize(typedData);
        }
    }
}
