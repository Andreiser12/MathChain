using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class CertificatesController : ApiControllerBase
    {
        private readonly ICertificateContext _certificateContext;

        public CertificatesController(ICertificateContext certificateContext)
        {
            _certificateContext = certificateContext;
        }

        [HttpPost("classroom/{classRoomId:guid}/certificates")]
        public async Task<IActionResult> IssueClassCertificates(Guid classRoomId, [FromBody] IssueCertificatesRequest? request)
        {
            var certificates = await _certificateContext.IssueClassCertificatesAsync(classRoomId, request);

            return Success(certificates, "Certificates issued successfully.", StatusCodes.Status201Created);
        }

        [HttpGet("classroom/{classRoomId:guid}/certificates")]
        public async Task<IActionResult> GetClassCertificates(Guid classRoomId)
        {
            var certificates = await _certificateContext.GetClassCertificatesAsync(classRoomId);

            return Success(certificates);
        }

        [HttpDelete("certificates/{certificateId:guid}")]
        public async Task<IActionResult> Revoke(Guid certificateId)
        {
            await _certificateContext.RevokeAsync(certificateId);

            return SuccessMessage("Certificate revoked successfully.");
        }

        [HttpGet("certificates/chapters/{chapterId:guid}/progress")]
        public async Task<IActionResult> GetChapterProgress(Guid chapterId)
        {
            var progress = await _certificateContext.GetChapterProgressAsync(chapterId);

            return Success(progress);
        }

        [HttpPost("certificates/chapters/{chapterId:guid}")]
        public async Task<IActionResult> ClaimChapterCertificate(Guid chapterId)
        {
            var certificate = await _certificateContext.ClaimChapterCertificateAsync(chapterId);

            return Success(certificate, "Chapter certificate issued successfully.", StatusCodes.Status201Created);
        }

        [HttpGet("certificates/mine")]
        public async Task<IActionResult> GetMine()
        {
            var certificates = await _certificateContext.GetMyCertificatesAsync();

            return Success(certificates);
        }

        [HttpGet("certificates/smart-account")]
        public async Task<IActionResult> GetMySmartAccount()
        {
            var account = await _certificateContext.GetMySmartAccountAsync();

            return Success(account);
        }

        [AllowAnonymous]
        [HttpGet("certificates/verify/{tokenId:long}")]
        public async Task<IActionResult> Verify(long tokenId, [FromQuery] string? key)
        {
            var verification = await _certificateContext.VerifyAsync(tokenId, key);

            return Success(verification);
        }

        [HttpGet("certificates/setup")]
        public async Task<IActionResult> GetSetup()
        {
            var setup = await _certificateContext.GetSetupAsync();

            return Success(setup);
        }

        [HttpGet("certificates/balance/{walletAddress}")]
        public async Task<IActionResult> GetBalance(string walletAddress)
        {
            var balance = await _certificateContext.GetBalanceAsync(walletAddress);

            return Success(balance);
        }
    }
}
