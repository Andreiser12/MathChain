using System.Text.Json;
using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.Solutions;
using MathChain.Tests.Shared.Fakes;

namespace MathChain.Application.UnitTests
{
    public class SolutionContextTests
    {
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string OtherStudent = "0x4444444444444444444444444444444444444444";
        private const string Teacher = "0x6666666666666666666666666666666666666666";
        private const string Admin = "0x7777777777777777777777777777777777777777";
        private const string Payer = "0x8888888888888888888888888888888888888888";
        private const string FormulaCode = "test-formula-f1";

        private readonly InMemoryCurriculumGateway _curriculum = new();
        private readonly InMemoryPracticeExerciseGateway _practice = new();
        private readonly InMemorySolutionPurchaseGateway _purchases = new();
        private readonly FakeSolutionPaymentRegistry _registry = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ManualTimeProvider _time = new();
        private readonly SolutionPaymentOptions _options = new() { ConfirmationWait = TimeSpan.Zero, PollInterval = TimeSpan.Zero };
        private readonly CurriculumFormula _formula;
        private EmbeddedSolutionCatalog _catalog;
        private SolutionContext _context;

        private static readonly object[] Templates =
        {
            new { difficulty = "Easy", order = 1, wolframQuery = "{a}+{b}", latexTemplate = "{a}+{b}", variables = new { a = new[] { 2, 2 }, b = new[] { 3, 3 } } },
            new { difficulty = "Medium", order = 2, wolframQuery = "{a}*2", latexTemplate = "{a}\\cdot 2", variables = new { a = new[] { 4, 4 } } },
            new { difficulty = "Hard", order = 3, wolframQuery = "{a}^2", latexTemplate = "{a}^2", variables = new { a = new[] { 5, 5 } } }
        };

        public SolutionContextTests()
        {
            _formula = new CurriculumFormula
            {
                Id = Guid.NewGuid(),
                ChapterId = Guid.NewGuid(),
                FormulaCode = FormulaCode,
                Title = "Test formula",
                ExercisesJson = JsonSerializer.Serialize(Templates)
            };
            _curriculum.Formulas.Add(_formula);
            _catalog = Catalog("{a}^2");
            _context = CreateContext();
            _currentUser.As(Student, UserRole.Student);
            _registry.Balances[Payer] = 10_000_000;
        }

        private SolutionContext CreateContext()
        {
            return new SolutionContext(_curriculum, _practice, _purchases, _registry, _catalog,
                new AccessGuard(_currentUser, new InMemoryClassRoomGateway()), _time, _options);
        }

        private static EmbeddedSolutionCatalog Catalog(string hardLatex)
        {
            var file = new
            {
                formulas = new Dictionary<string, object[]>
                {
                    [FormulaCode] = new object[]
                    {
                        new
                        {
                            order = 1,
                            latexTemplate = "{a}+{b}",
                            wolframQuery = "{a}+{b}",
                            steps = new[]
                            {
                                new { text = "Add {a} and {b}.", latex = "{a}+{b}={a+b}" },
                                new { text = "The result is {a+b}.", latex = (string?)null }
                            }
                        },
                        new
                        {
                            order = 3,
                            latexTemplate = hardLatex,
                            wolframQuery = "{a}^2",
                            steps = new[] { new { text = "Square {a}.", latex = "{a}^2={a*a}" } }
                        }
                    }
                }
            };

            return EmbeddedSolutionCatalog.FromJson(new[] { JsonSerializer.Serialize(file) });
        }

        private Task<SolutionQuoteDto> QuoteAsync(int index = 2, string payer = Payer)
        {
            return _context.CreateQuoteAsync(_formula.Id, index, new CreateSolutionQuoteRequest { PayerAddress = payer });
        }

        private Task<SolutionPurchaseDto> PayAsync(Guid purchaseId, string signature = FakeSolutionPaymentRegistry.ValidSignature)
        {
            return _context.SubmitPaymentAsync(purchaseId, new SubmitSolutionPaymentRequest { Signature = signature });
        }

        private PracticeExercise AddExercise(int index, string? values, string wallet = Student)
        {
            var exercise = new PracticeExercise
            {
                Id = Guid.NewGuid(),
                StudentWallet = wallet,
                FormulaId = _formula.Id,
                ExerciseIndex = index,
                Difficulty = "Easy",
                Requirement = "Compute",
                Latex = "2+3",
                Form = AnswerForm.Number,
                ExpectedAnswer = "5",
                VariableValues = values,
                CreatedAt = _time.Now.UtcDateTime,
                ExpiresAt = _time.Now.UtcDateTime.AddHours(2)
            };
            _practice.Exercises.Add(exercise);
            return exercise;
        }

        private void AddCompletedPurchase(int index, string wallet = Student)
        {
            _purchases.Purchases.Add(new SolutionPurchase
            {
                Id = Guid.NewGuid(),
                AccountWallet = wallet,
                FormulaId = _formula.Id,
                ExerciseIndex = index,
                Status = SolutionPurchaseStatus.Completed
            });
        }

        [Fact]
        public async Task GetOfferAsync_ShouldListThePricesSolutionsAndPurchases_WhenTheStudentHasAPurchase()
        {
            AddCompletedPurchase(0);

            var offer = await _context.GetOfferAsync(_formula.Id);

            Assert.False(offer.FreeAccess);
            Assert.True(offer.CanPurchase);
            Assert.Equal("USDC", offer.TokenSymbol);
            Assert.Equal(new decimal?[] { 1m, 2m, 3m }, offer.Exercises.Select(e => e.Price));
            Assert.Equal(new[] { true, false, true }, offer.Exercises.Select(e => e.HasSolution));
            Assert.Equal(new[] { true, false, false }, offer.Exercises.Select(e => e.Purchased));
            Assert.Equal(new[] { "Easy", "Medium", "Hard" }, offer.Exercises.Select(e => e.Difficulty));
        }

        [Theory]
        [InlineData(UserRole.Teacher)]
        [InlineData(UserRole.Admin)]
        public async Task GetOfferAsync_ShouldOpenTheSolutionsForFree_WhenTheUserIsATeacherOrAnAdmin(UserRole role)
        {
            _currentUser.As(Teacher, role);

            var offer = await _context.GetOfferAsync(_formula.Id);

            Assert.True(offer.FreeAccess);
            Assert.False(offer.CanPurchase);
            Assert.All(offer.Exercises, e => Assert.Null(e.Price));
        }

        [Fact]
        public async Task GetOfferAsync_ShouldShowTheSolutionsWithoutPrices_WhenTheNetworkIsDown()
        {
            _registry.PricesUnavailable = true;

            var offer = await _context.GetOfferAsync(_formula.Id);

            Assert.False(offer.CanPurchase);
            Assert.All(offer.Exercises, e => Assert.Null(e.Price));
            Assert.True(offer.Exercises[0].HasSolution);
        }

        [Fact]
        public async Task GetOfferAsync_ShouldHaveNoSolutions_WhenTheFormulaUsesGenericExercises()
        {
            _formula.ExercisesJson = null;

            var offer = await _context.GetOfferAsync(_formula.Id);

            Assert.Equal(9, offer.Exercises.Count);
            Assert.All(offer.Exercises, e => Assert.False(e.HasSolution));
        }

        [Fact]
        public async Task GetOfferAsync_ShouldRejectTheRequest_WhenTheUserIsAnonymousOrTheFormulaDoesNotExist()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetOfferAsync(Guid.NewGuid()));

            _currentUser.Anonymous();
            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.GetOfferAsync(_formula.Id));
        }

        [Fact]
        public async Task CreateQuoteAsync_ShouldStoreTheQuoteAndReturnTheDataToSign_WhenTheStudentAsksForAQuote()
        {
            var quote = await QuoteAsync();

            var stored = Assert.Single(_purchases.Purchases);
            Assert.Equal(quote.PurchaseId, stored.Id);
            Assert.Equal(Student, stored.AccountWallet);
            Assert.Equal(Payer, stored.PayerAddress);
            Assert.Equal(2, stored.ExerciseIndex);
            Assert.Equal(SolutionTier.Hard, stored.Tier);
            Assert.Equal(3_000_000, stored.PriceUnits);
            Assert.Equal(SolutionPurchaseStatus.Quoted, stored.Status);
            Assert.Equal(_registry.ComputeSolutionId(_formula.Id, 2), stored.SolutionId);
            Assert.Equal(_registry.ComputeNonce(Student, stored.SolutionId, SolutionTier.Hard, stored.Salt), stored.Nonce);
            Assert.Equal(_time.Now.UtcDateTime + _options.QuoteLifetime, stored.ValidBefore);

            Assert.Equal(3m, quote.Price);
            Assert.Equal(10m, quote.PayerBalance);
            Assert.Equal("Hard", quote.Difficulty);
            Assert.Equal(84532, quote.ChainId);

            using var typedData = JsonDocument.Parse(quote.TypedData);
            var root = typedData.RootElement;
            Assert.Equal("ReceiveWithAuthorization", root.GetProperty("primaryType").GetString());
            Assert.Equal("USDC", root.GetProperty("domain").GetProperty("name").GetString());
            Assert.Equal("2", root.GetProperty("domain").GetProperty("version").GetString());
            Assert.Equal(84532, root.GetProperty("domain").GetProperty("chainId").GetInt64());
            Assert.Equal(FakeSolutionPaymentRegistry.TokenAddress, root.GetProperty("domain").GetProperty("verifyingContract").GetString());
            var message = root.GetProperty("message");
            Assert.Equal(Payer, message.GetProperty("from").GetString());
            Assert.Equal(FakeSolutionPaymentRegistry.ContractAddress, message.GetProperty("to").GetString());
            Assert.Equal("3000000", message.GetProperty("value").GetString());
            Assert.Equal("0", message.GetProperty("validAfter").GetString());
            Assert.Equal(new DateTimeOffset(stored.ValidBefore, TimeSpan.Zero).ToUnixTimeSeconds().ToString(), message.GetProperty("validBefore").GetString());
            Assert.Equal(stored.Nonce, message.GetProperty("nonce").GetString());
            Assert.Equal(6, root.GetProperty("types").GetProperty("ReceiveWithAuthorization").GetArrayLength());
        }

        [Theory]
        [InlineData("")]
        [InlineData("0x123")]
        [InlineData("0xzz88888888888888888888888888888888888888")]
        public async Task CreateQuoteAsync_ShouldThrowBusinessRuleException_WhenThePayerHasNoWalletAddress(string payer)
        {
            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => QuoteAsync(payer: payer));

            Assert.Equal("Connect a wallet to pay for the solution.", error.Message);
            Assert.Empty(_purchases.Purchases);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(-1)]
        public async Task CreateQuoteAsync_ShouldThrowNotFoundException_WhenTheExerciseHasNoSolution(int index)
        {
            var error = await Assert.ThrowsAsync<NotFoundException>(() => QuoteAsync(index));

            Assert.Equal("This exercise does not have a step-by-step solution yet.", error.Message);
        }

        [Fact]
        public async Task CreateQuoteAsync_ShouldThrowBusinessRuleException_WhenTheSolutionIsAlreadyOwned()
        {
            AddCompletedPurchase(2);

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => QuoteAsync());

            Assert.Equal("You already own this solution.", error.Message);
        }

        [Fact]
        public async Task CreateQuoteAsync_ShouldRejectTheQuote_WhenAPaymentIsBeingProcessed()
        {
            var first = await QuoteAsync();
            _purchases.Purchases.Single().Status = SolutionPurchaseStatus.Submitted;

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => QuoteAsync());

            Assert.StartsWith("A payment for this solution is already being processed.", error.Message);
            Assert.Equal(first.PurchaseId, Assert.Single(_purchases.Purchases).Id);
        }

        [Fact]
        public async Task CreateQuoteAsync_ShouldRecordTheOwnership_WhenItIsPaidOnChainButMissingInTheDatabase()
        {
            _registry.PurchasedOnChain.Add(FakeSolutionPaymentRegistry.Key(Student, _registry.ComputeSolutionId(_formula.Id, 2)));

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => QuoteAsync());

            Assert.Equal("You already own this solution.", error.Message);
            Assert.Equal(SolutionPurchaseStatus.Completed, Assert.Single(_purchases.Purchases).Status);
            Assert.Contains(2, (await _context.GetOfferAsync(_formula.Id)).Exercises.Where(e => e.Purchased).Select(e => e.Index));
        }

        [Fact]
        public async Task CreateQuoteAsync_ShouldNotCharge_WhenTheUserHasFreeAccess()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<BusinessRuleException>(() => QuoteAsync());
            Assert.Empty(_purchases.Purchases);
        }

        [Fact]
        public async Task CreateQuoteAsync_ShouldThrowServiceUnavailableException_WhenThereIsNoRelayer()
        {
            _registry.CanRelayValue = false;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => QuoteAsync());
            Assert.Empty(_purchases.Purchases);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldCompleteThePurchase_WhenTheTransactionIsConfirmed()
        {
            var quote = await QuoteAsync();

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.CompletedStatus, result.Status);
            Assert.Equal("https://explorer.example/tx/" + result.TransactionHash, result.TransactionUrl);
            var stored = Assert.Single(_purchases.Purchases);
            Assert.Equal(SolutionPurchaseStatus.Completed, stored.Status);
            Assert.Equal(_time.Now.UtcDateTime, stored.CompletedAt);
            Assert.Equal(result.TransactionHash, stored.TransactionHash);

            var order = Assert.Single(_registry.SubmittedOrders);
            Assert.Equal(Student, order.AccountAddress);
            Assert.Equal(stored.SolutionId, order.SolutionId);
            Assert.Equal(SolutionTier.Hard, order.Tier);
            Assert.Equal(stored.Salt, order.Salt);
            Assert.Equal(Payer, order.PayerAddress);
            Assert.Equal(3_000_000, order.PriceUnits);
            Assert.Equal(new DateTimeOffset(stored.ValidBefore, TimeSpan.Zero).ToUnixTimeSeconds(), order.ValidBefore);
            Assert.Equal(FakeSolutionPaymentRegistry.ValidSignature, order.Signature);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldNotPayTwice_WhenItIsCalledASecondTime()
        {
            var quote = await QuoteAsync();
            await PayAsync(quote.PurchaseId);

            var again = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.CompletedStatus, again.Status);
            Assert.Single(_registry.SubmittedOrders);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldNotSendAgain_WhenAParallelRequestAlreadyStarted()
        {
            var quote = await QuoteAsync();
            _purchases.AfterRead = stored =>
            {
                stored.Status = SolutionPurchaseStatus.Submitting;
                _purchases.AfterRead = null;
            };

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.PendingStatus, result.Status);
            Assert.Empty(_registry.SubmittedOrders);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldKeepTheQuoteOpen_WhenTheSignatureIsFromAnotherWallet()
        {
            var quote = await QuoteAsync();

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => PayAsync(quote.PurchaseId, "0xother"));

            Assert.Equal("The signature does not match the paying wallet. Sign the payment again.", error.Message);
            Assert.Equal(SolutionPurchaseStatus.Quoted, Assert.Single(_purchases.Purchases).Status);
            Assert.Empty(_registry.SubmittedOrders);
            Assert.Equal(SolutionContext.CompletedStatus, (await PayAsync(quote.PurchaseId)).Status);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldFailWithoutSending_WhenThePaymentWindowHasPassed()
        {
            var quote = await QuoteAsync();
            _time.Now = _time.Now + _options.QuoteLifetime;

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => PayAsync(quote.PurchaseId));

            Assert.Equal("The payment window has expired. Open it again.", error.Message);
            Assert.Equal(SolutionPurchaseStatus.Failed, Assert.Single(_purchases.Purchases).Status);
            Assert.Empty(_registry.SubmittedOrders);
            await QuoteAsync();
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldFailWithTheReason_WhenTheContractRejectsThePayment()
        {
            var quote = await QuoteAsync();
            _registry.SubmitError = new BusinessRuleException("The paying wallet does not have enough USDC.");

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => PayAsync(quote.PurchaseId));

            Assert.Equal("The paying wallet does not have enough USDC.", error.Message);
            var stored = Assert.Single(_purchases.Purchases);
            Assert.Equal(SolutionPurchaseStatus.Failed, stored.Status);
            Assert.Equal(error.Message, stored.FailureReason);

            _registry.SubmitError = null;
            var again = await Assert.ThrowsAsync<BusinessRuleException>(() => PayAsync(quote.PurchaseId));
            Assert.Equal(error.Message, again.Message);
            Assert.Empty(_registry.SubmittedOrders);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldLetTheStudentRetry_WhenTheNetworkFails()
        {
            var quote = await QuoteAsync();
            _registry.SubmitError = new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => PayAsync(quote.PurchaseId));
            Assert.Equal(SolutionPurchaseStatus.Quoted, Assert.Single(_purchases.Purchases).Status);

            _registry.SubmitError = null;
            Assert.Equal(SolutionContext.CompletedStatus, (await PayAsync(quote.PurchaseId)).Status);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldCompleteWithoutPayingAgain_WhenItIsAlreadyPaidOnChain()
        {
            var quote = await QuoteAsync();
            var stored = _purchases.Purchases.Single();
            _registry.PurchasedOnChain.Add(FakeSolutionPaymentRegistry.Key(Student, stored.SolutionId));
            _registry.SubmitError = new SolutionAlreadyPurchasedException();

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.CompletedStatus, result.Status);
            Assert.Equal(SolutionPurchaseStatus.Completed, stored.Status);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldFailAndUnlockNothing_WhenTheTransactionIsReverted()
        {
            var quote = await QuoteAsync();
            _registry.NextOutcome = SolutionPaymentState.Reverted;

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.FailedStatus, result.Status);
            Assert.Equal("The payment transaction failed. No money was taken. Try again.", result.Message);
            Assert.Equal(SolutionPurchaseStatus.Failed, _purchases.Purchases.Single().Status);
            Assert.DoesNotContain((await _context.GetOfferAsync(_formula.Id)).Exercises, e => e.Purchased);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldComplete_WhenItRevertedBecauseSomeoneElseRelayedIt()
        {
            var quote = await QuoteAsync();
            var stored = _purchases.Purchases.Single();
            _registry.NextOutcome = SolutionPaymentState.Reverted;
            _registry.PurchasedOnChain.Add(FakeSolutionPaymentRegistry.Key(Student, stored.SolutionId));

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.CompletedStatus, result.Status);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldConfirmThePurchaseLater_WhenTheTransactionIsStillPending()
        {
            var quote = await QuoteAsync();
            _registry.NextOutcome = SolutionPaymentState.Pending;

            var pending = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.PendingStatus, pending.Status);
            Assert.NotNull(pending.TransactionHash);
            Assert.Equal(SolutionPurchaseStatus.Submitted, _purchases.Purchases.Single().Status);
            await Assert.ThrowsAsync<BusinessRuleException>(() => QuoteAsync());

            _registry.ConfirmLater(pending.TransactionHash!);
            var confirmed = await _context.GetPurchaseAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.CompletedStatus, confirmed.Status);
            Assert.Equal(SolutionPurchaseStatus.Completed, _purchases.Purchases.Single().Status);
            Assert.Single(_registry.SubmittedOrders);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldWaitForTheConfirmation_WhenTheOutcomeIsNotKnownYet()
        {
            _options.ConfirmationWait = TimeSpan.FromSeconds(30);
            _registry.PendingPollsBeforeOutcome = 2;
            var quote = await QuoteAsync();

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.CompletedStatus, result.Status);
            Assert.Equal(3, _registry.OutcomeQueries);
        }

        [Fact]
        public async Task SubmitPaymentAsync_ShouldNotAcceptTheEvent_WhenItIsForADifferentPurchase()
        {
            var quote = await QuoteAsync();
            _registry.ChangeOutcome = (_, outcome) =>
            {
                outcome.PriceUnits = 1_000_000;
                return outcome;
            };

            var result = await PayAsync(quote.PurchaseId);

            Assert.Equal(SolutionContext.FailedStatus, result.Status);
            Assert.Equal(SolutionPurchaseStatus.Failed, _purchases.Purchases.Single().Status);
        }

        [Fact]
        public async Task PurchaseAccess_ShouldThrowNotFoundException_WhenThePurchaseBelongsToAnotherAccount()
        {
            var quote = await QuoteAsync();
            _currentUser.As(OtherStudent, UserRole.Student);

            await Assert.ThrowsAsync<NotFoundException>(() => PayAsync(quote.PurchaseId));
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetPurchaseAsync(quote.PurchaseId));
            Assert.Empty(_registry.SubmittedOrders);
        }

        [Fact]
        public async Task GetSolutionAsync_ShouldUseTheValuesOfTheExercise_WhenTheSolutionIsPaid()
        {
            AddCompletedPurchase(0);
            var exercise = AddExercise(0, "{\"a\":7,\"b\":11}");

            var solution = await _context.GetSolutionAsync(exercise.Id);

            Assert.Equal(new[] { "Add 7 and 11.", "The result is 18." }, solution.Steps.Select(s => s.Text));
            Assert.Equal(new[] { "7+11=18", null }, solution.Steps.Select(s => s.Latex));
            Assert.Equal("5", solution.Answer);
            Assert.Equal(AnswerForm.Number, solution.Form);
        }

        [Fact]
        public async Task GetSolutionAsync_ShouldThrowForbiddenException_WhenTheStudentHasNotPaid()
        {
            var exercise = AddExercise(0, "{\"a\":7,\"b\":11}");
            AddCompletedPurchase(0, OtherStudent);

            var error = await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetSolutionAsync(exercise.Id));

            Assert.Equal("Pay for this solution to see it.", error.Message);
        }

        [Theory]
        [InlineData(UserRole.Teacher)]
        [InlineData(UserRole.Admin)]
        public async Task GetSolutionAsync_ShouldShowTheSolutionForFree_WhenTheUserIsATeacherOrAnAdmin(UserRole role)
        {
            _currentUser.As(Teacher, role);
            var exercise = AddExercise(2, "{\"a\":5}", Teacher);

            var solution = await _context.GetSolutionAsync(exercise.Id);

            Assert.Equal("5^2=25", solution.Steps.Single().Latex);
        }

        [Fact]
        public async Task GetSolutionAsync_ShouldReportNotFound_WhenTheExerciseBelongsToAnotherStudent()
        {
            AddCompletedPurchase(0);
            var exercise = AddExercise(0, "{\"a\":7,\"b\":11}", OtherStudent);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetSolutionAsync(exercise.Id));
        }

        [Fact]
        public async Task GetSolutionAsync_ShouldAskToReload_WhenTheExerciseWasCreatedBeforeValuesWereStored()
        {
            AddCompletedPurchase(0);
            var exercise = AddExercise(0, null);

            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.GetSolutionAsync(exercise.Id));

            Assert.Equal("Load the exercise again to see its solution.", error.Message);
        }

        [Fact]
        public async Task GetSolutionAsync_ShouldHaveNoSolution_WhenTheTemplateChangedAfterTheSolutionWasWritten()
        {
            _catalog = Catalog("{a}^{2}");
            _context = CreateContext();
            _currentUser.As(Admin, UserRole.Admin);
            var exercise = AddExercise(2, "{\"a\":5}", Admin);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetSolutionAsync(exercise.Id));
            Assert.False((await _context.GetOfferAsync(_formula.Id)).Exercises[2].HasSolution);
        }

        [Fact]
        public async Task GetPaymentSetupAsync_ShouldShowThePricesAndTheRelayerOnlyToAdmins_WhenTheSetupIsRequested()
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetPaymentSetupAsync());

            _currentUser.As(Admin, UserRole.Admin);
            var setup = await _context.GetPaymentSetupAsync();

            Assert.True(setup.CanRelay);
            Assert.Equal((1m, 2m, 3m), (setup.EasyPrice, setup.MediumPrice, setup.HardPrice));
            Assert.Equal(FakeSolutionPaymentRegistry.RelayerAddress, setup.RelayerAddress);
            Assert.Equal(FakeSolutionPaymentRegistry.ContractAddress, setup.ContractAddress);
        }

        [Fact]
        public async Task GetPaymentSetupAsync_ShouldReportThatPricesCannotBeRead_WhenTheContractIsNotConfigured()
        {
            _currentUser.As(Admin, UserRole.Admin);
            _registry.CanRead = false;

            var setup = await _context.GetPaymentSetupAsync();

            Assert.False(setup.CanRead);
            Assert.Null(setup.EasyPrice);
        }

        [Theory]
        [InlineData("Easy", 1.5, "setPrice:Easy:1500000")]
        [InlineData("medium", 2, "setPrice:Medium:2000000")]
        [InlineData("Hard", 0.000001, "setPrice:Hard:1")]
        public void BuildPriceChange_ShouldConvertThePriceToTokenUnits_WhenAnAdminChangesAPrice(string difficulty, double price, string expectedData)
        {
            _currentUser.As(Admin, UserRole.Admin);

            var change = _context.BuildPriceChange(new ChangeSolutionPriceRequest { Difficulty = difficulty, Price = (decimal)price });

            Assert.Equal(expectedData, change.Data);
            Assert.Equal(FakeSolutionPaymentRegistry.ContractAddress, change.To);
        }

        [Theory]
        [InlineData("Easy", "0")]
        [InlineData("Easy", "-1")]
        [InlineData("Easy", "1000.5")]
        [InlineData("Easy", "0.0000001")]
        [InlineData("Expert", "1")]
        [InlineData("", "1")]
        public void BuildPriceChange_ShouldThrowBusinessRuleException_WhenThePriceIsInvalid(string difficulty, string price)
        {
            _currentUser.As(Admin, UserRole.Admin);

            Assert.Throws<BusinessRuleException>(() => _context.BuildPriceChange(new ChangeSolutionPriceRequest
            {
                Difficulty = difficulty,
                Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)
            }));
        }

        [Fact]
        public void BuildPriceChange_ShouldThrowForbiddenException_WhenTheUserIsNotAnAdmin()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            Assert.Throws<ForbiddenException>(() => _context.BuildPriceChange(new ChangeSolutionPriceRequest { Difficulty = "Easy", Price = 1 }));
        }
    }
}
