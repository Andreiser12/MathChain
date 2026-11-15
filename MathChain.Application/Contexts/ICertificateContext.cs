using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface ICertificateContext
    {
        Task<List<ClassCertificateDto>> IssueClassCertificatesAsync(Guid classRoomId, IssueCertificatesRequest? request);
        Task<List<ClassCertificateDto>> GetClassCertificatesAsync(Guid classRoomId);
        Task RevokeAsync(Guid certificateId);
        Task<ChapterCertificateProgressDto> GetChapterProgressAsync(Guid chapterId);
        Task<MyCertificateDto> ClaimChapterCertificateAsync(Guid chapterId);
        Task<List<MyCertificateDto>> GetMyCertificatesAsync();
        Task<CertificateVerificationDto> VerifyAsync(long tokenId, string? verificationKey);
        Task<CertificateSetupDto> GetSetupAsync();
        Task<decimal> GetBalanceAsync(string walletAddress);
    }
}
