using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Ipfs;
using MathChain.Infrastructure.ResponseHandling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace MathChain.API.Controllers
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class IpfsController : ControllerBase
    {
        private const int MaximumRequestSize = IIpfsStorageService.MaximumFileSize + 1024 * 1024;
        private const int MaximumFileNameLength = 200;
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        private readonly IIpfsStorageService _ipfsService;

        public IpfsController(IIpfsStorageService ipfsService)
        {
            _ipfsService = ipfsService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaximumRequestSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestSize)]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            var file = request?.File;

            if (file == null || file.Length == 0)
            {
                return BadRequest(EnvelopeExtensions.Failure<string>("No file uploaded.", 400));
            }

            if (file.Length > IIpfsStorageService.MaximumFileSize)
            {
                return BadRequest(EnvelopeExtensions.Failure<string>("Files can be at most 10 MB.", 400));
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var ipfsHash = await _ipfsService.SaveFileAsync(memoryStream.ToArray());

            return Ok(EnvelopeExtensions.Success(ipfsHash, "File stored successfully on IPFS node."));
        }

        [AllowAnonymous]
        [HttpGet("download/{hash}")]
        [HttpGet("{hash}")]
        public async Task<IActionResult> Download(string hash, [FromQuery] string? fileName)
        {
            if (!ContentId.TryParse(hash, out _))
            {
                throw new BusinessRuleException("The content identifier is not valid.");
            }

            var fileBytes = await _ipfsService.GetFileAsync(hash);

            if (fileBytes == null)
            {
                throw new NotFoundException($"File not found on IPFS storage (hash: {hash}).");
            }

            var downloadName = SafeFileName(fileName) ?? hash;

            if (!ContentTypes.TryGetContentType(downloadName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

            return File(fileBytes, contentType, downloadName, lastModified: null, entityTag: new EntityTagHeaderValue($"\"{hash}\""));
        }

        private static string? SafeFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var name = new string(Path.GetFileName(fileName.Replace('\\', '/')).Where(c => !char.IsControl(c)).ToArray()).Trim();

            if (name.Length == 0 || name == "." || name == "..")
            {
                return null;
            }

            return name.Length > MaximumFileNameLength ? name[..MaximumFileNameLength] : name;
        }
    }
}
