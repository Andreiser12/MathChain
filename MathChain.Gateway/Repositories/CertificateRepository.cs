using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class CertificateRepository : BaseRepository, ICertificateGateway
    {
        public CertificateRepository(MathChainDbContext context) : base(context)
        {
        }

        public async Task<Certificate?> GetByIdAsync(Guid id)
        {
            return await _context.Certificates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Certificate?> GetByTokenIdAsync(long tokenId)
        {
            return await _context.Certificates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TokenId == tokenId);
        }

        public async Task<List<Certificate>> GetBySubjectAsync(CertificateKind kind, Guid subjectId)
        {
            return await _context.Certificates
                .AsNoTracking()
                .Where(c => c.Kind == kind && c.SubjectId == subjectId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Certificate>> GetByStudentAsync(string studentWallet)
        {
            var wallet = studentWallet.Trim().ToLower();

            return await _context.Certificates
                .AsNoTracking()
                .Where(c => c.StudentWallet == wallet)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IReadOnlyList<Certificate> certificates)
        {
            _context.Certificates.AddRange(certificates);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        public async Task UpdateRangeAsync(IReadOnlyList<Certificate> certificates)
        {
            _context.Certificates.UpdateRange(certificates);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
    }
}
