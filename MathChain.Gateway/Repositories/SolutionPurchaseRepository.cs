using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MathChain.Gateway.Repositories
{
    public class SolutionPurchaseRepository : BaseRepository, ISolutionPurchaseGateway
    {
        public SolutionPurchaseRepository(MathChainDbContext context) : base(context)
        {
        }

        public async Task<SolutionPurchase?> GetByIdAsync(Guid id)
        {
            return await _context.SolutionPurchases
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(SolutionPurchase purchase)
        {
            _context.SolutionPurchases.Add(purchase);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
                && purchase.Status == SolutionPurchaseStatus.Completed)
            {
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }

        public async Task<List<int>> GetPurchasedIndicesAsync(string accountWallet, Guid formulaId)
        {
            return await _context.SolutionPurchases
                .AsNoTracking()
                .Where(p => p.AccountWallet == accountWallet && p.FormulaId == formulaId && p.Status == SolutionPurchaseStatus.Completed)
                .Select(p => p.ExerciseIndex)
                .Distinct()
                .OrderBy(i => i)
                .ToListAsync();
        }

        public async Task<bool> HasInProgressAsync(string accountWallet, Guid formulaId, int exerciseIndex)
        {
            return await _context.SolutionPurchases
                .AsNoTracking()
                .AnyAsync(p => p.AccountWallet == accountWallet
                    && p.FormulaId == formulaId
                    && p.ExerciseIndex == exerciseIndex
                    && (p.Status == SolutionPurchaseStatus.Submitting || p.Status == SolutionPurchaseStatus.Submitted));
        }

        public async Task<bool> TryStartSubmittingAsync(Guid id)
        {
            var updated = await _context.SolutionPurchases
                .Where(p => p.Id == id && p.Status == SolutionPurchaseStatus.Quoted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, SolutionPurchaseStatus.Submitting));

            return updated == 1;
        }

        public async Task ReturnToQuotedAsync(Guid id)
        {
            await _context.SolutionPurchases
                .Where(p => p.Id == id && p.Status == SolutionPurchaseStatus.Submitting)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, SolutionPurchaseStatus.Quoted));
        }

        public async Task MarkSubmittedAsync(Guid id, string transactionHash)
        {
            await _context.SolutionPurchases
                .Where(p => p.Id == id && p.Status == SolutionPurchaseStatus.Submitting)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, SolutionPurchaseStatus.Submitted)
                    .SetProperty(p => p.TransactionHash, transactionHash));
        }

        public async Task<bool> TryCompleteAsync(Guid id, DateTime completedAt)
        {
            try
            {
                var updated = await _context.SolutionPurchases
                    .Where(p => p.Id == id && p.Status != SolutionPurchaseStatus.Completed)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Status, SolutionPurchaseStatus.Completed)
                        .SetProperty(p => p.CompletedAt, completedAt)
                        .SetProperty(p => p.FailureReason, (string?)null));

                return updated == 1;
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return false;
            }
        }

        public async Task MarkFailedAsync(Guid id, string reason)
        {
            var storedReason = reason.Length > 500 ? reason[..500] : reason;

            await _context.SolutionPurchases
                .Where(p => p.Id == id && p.Status != SolutionPurchaseStatus.Completed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, SolutionPurchaseStatus.Failed)
                    .SetProperty(p => p.FailureReason, storedReason));
        }
    }
}
