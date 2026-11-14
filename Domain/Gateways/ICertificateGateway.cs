using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Domain.Gateways
{
    public interface ICertificateGateway
    {
        Task<Certificate?> GetByIdAsync(Guid id);
        Task<Certificate?> GetByTokenIdAsync(long tokenId);
        Task<List<Certificate>> GetBySubjectAsync(CertificateKind kind, Guid subjectId);
        Task<List<Certificate>> GetByStudentAsync(string studentWallet);
        Task AddRangeAsync(IReadOnlyList<Certificate> certificates);
        Task UpdateRangeAsync(IReadOnlyList<Certificate> certificates);
    }
}
