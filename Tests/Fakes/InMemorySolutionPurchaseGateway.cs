using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemorySolutionPurchaseGateway : ISolutionPurchaseGateway
    {
        public List<SolutionPurchase> Purchases { get; } = new();
        public Action<SolutionPurchase>? AfterRead { get; set; }

        public Task<SolutionPurchase?> GetByIdAsync(Guid id)
        {
            var stored = Purchases.FirstOrDefault(p => p.Id == id);
            var copy = EntityCopy.Detached(stored);

            if (stored != null)
            {
                AfterRead?.Invoke(stored);
            }

            return Task.FromResult(copy);
        }

        public Task AddAsync(SolutionPurchase purchase)
        {
            if (purchase.Status == SolutionPurchaseStatus.Completed && IsCompleted(purchase.AccountWallet, purchase.FormulaId, purchase.ExerciseIndex))
            {
                return Task.CompletedTask;
            }

            Purchases.Add(EntityCopy.Detached(purchase)!);
            return Task.CompletedTask;
        }

        public Task<List<int>> GetPurchasedIndicesAsync(string accountWallet, Guid formulaId)
        {
            return Task.FromResult(Purchases
                .Where(p => p.AccountWallet == accountWallet && p.FormulaId == formulaId && p.Status == SolutionPurchaseStatus.Completed)
                .Select(p => p.ExerciseIndex)
                .Distinct()
                .OrderBy(i => i)
                .ToList());
        }

        public Task<bool> HasInProgressAsync(string accountWallet, Guid formulaId, int exerciseIndex)
        {
            return Task.FromResult(Purchases.Any(p => p.AccountWallet == accountWallet && p.FormulaId == formulaId
                && p.ExerciseIndex == exerciseIndex
                && (p.Status == SolutionPurchaseStatus.Submitting || p.Status == SolutionPurchaseStatus.Submitted)));
        }

        public Task<bool> TryStartSubmittingAsync(Guid id)
        {
            var purchase = Purchases.FirstOrDefault(p => p.Id == id && p.Status == SolutionPurchaseStatus.Quoted);

            if (purchase == null)
            {
                return Task.FromResult(false);
            }

            purchase.Status = SolutionPurchaseStatus.Submitting;
            return Task.FromResult(true);
        }

        public Task ReturnToQuotedAsync(Guid id)
        {
            var purchase = Purchases.FirstOrDefault(p => p.Id == id && p.Status == SolutionPurchaseStatus.Submitting);

            if (purchase != null)
            {
                purchase.Status = SolutionPurchaseStatus.Quoted;
            }

            return Task.CompletedTask;
        }

        public Task MarkSubmittedAsync(Guid id, string transactionHash)
        {
            var purchase = Purchases.FirstOrDefault(p => p.Id == id && p.Status == SolutionPurchaseStatus.Submitting);

            if (purchase != null)
            {
                purchase.Status = SolutionPurchaseStatus.Submitted;
                purchase.TransactionHash = transactionHash;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TryCompleteAsync(Guid id, DateTime completedAt)
        {
            var purchase = Purchases.FirstOrDefault(p => p.Id == id && p.Status != SolutionPurchaseStatus.Completed);

            if (purchase == null || IsCompleted(purchase.AccountWallet, purchase.FormulaId, purchase.ExerciseIndex))
            {
                return Task.FromResult(false);
            }

            purchase.Status = SolutionPurchaseStatus.Completed;
            purchase.CompletedAt = completedAt;
            purchase.FailureReason = null;
            return Task.FromResult(true);
        }

        public Task MarkFailedAsync(Guid id, string reason)
        {
            var purchase = Purchases.FirstOrDefault(p => p.Id == id && p.Status != SolutionPurchaseStatus.Completed);

            if (purchase != null)
            {
                purchase.Status = SolutionPurchaseStatus.Failed;
                purchase.FailureReason = reason;
            }

            return Task.CompletedTask;
        }

        private bool IsCompleted(string accountWallet, Guid formulaId, int exerciseIndex)
        {
            return Purchases.Any(p => p.AccountWallet == accountWallet && p.FormulaId == formulaId
                && p.ExerciseIndex == exerciseIndex && p.Status == SolutionPurchaseStatus.Completed);
        }
    }
}
