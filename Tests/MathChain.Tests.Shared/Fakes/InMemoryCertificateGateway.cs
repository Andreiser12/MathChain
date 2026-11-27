using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Shared.Fakes
{
    public class InMemoryCertificateGateway : ICertificateGateway
    {
        public List<Certificate> Certificates { get; } = new();

        public Task<Certificate?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(EntityCopy.Detached(Certificates.FirstOrDefault(c => c.Id == id)));
        }

        public Task<Certificate?> GetByTokenIdAsync(long tokenId)
        {
            return Task.FromResult(EntityCopy.Detached(Certificates.FirstOrDefault(c => c.TokenId == tokenId)));
        }

        public Task<List<Certificate>> GetBySubjectAsync(CertificateKind kind, Guid subjectId)
        {
            return Task.FromResult(Certificates
                .Where(c => c.Kind == kind && c.SubjectId == subjectId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => EntityCopy.Detached(c)!)
                .ToList());
        }

        public Task<List<Certificate>> GetByStudentAsync(string studentWallet)
        {
            var wallet = studentWallet.Trim().ToLower();

            return Task.FromResult(Certificates
                .Where(c => c.StudentWallet == wallet)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => EntityCopy.Detached(c)!)
                .ToList());
        }

        public Task AddRangeAsync(IReadOnlyList<Certificate> certificates)
        {
            Certificates.AddRange(certificates.Select(c => EntityCopy.Detached(c)!));
            return Task.CompletedTask;
        }

        public Task UpdateRangeAsync(IReadOnlyList<Certificate> certificates)
        {
            foreach (var certificate in certificates)
            {
                var index = Certificates.FindIndex(c => c.Id == certificate.Id);

                if (index < 0)
                {
                    throw new InvalidOperationException($"Certificate {certificate.Id} does not exist.");
                }

                Certificates[index] = EntityCopy.Detached(certificate)!;
            }

            return Task.CompletedTask;
        }
    }
}
