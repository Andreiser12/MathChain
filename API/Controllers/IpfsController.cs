using MathChain.Infrastructure.Ipfs;
using MathChain.Infrastructure.ResponseHandling;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IIpfsStorageService _ipfsService;
        public IpfsController(IIpfsStorageService ipfsService)
        {
            _ipfsService = ipfsService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            var file = request?.File;

            if (file == null || file.Length == 0)
            {
                return BadRequest(EnvelopeExtensions.Failure<string>("No file uploaded.", 400));
            }

            using var memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            byte[] fileBytes = memoryStream.ToArray();

            var result = await _ipfsService.SaveFileAsync(fileBytes);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("download/{hash}")]
        [HttpGet("{hash}")]
        public async Task<IActionResult> Download(string hash, [FromQuery] string? fileName)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return BadRequest("IPFS hash is required.");
            }

            var fileBytes = await _ipfsService.GetFileAsync(hash);

            if (fileBytes == null)
            {
                return NotFound("File not found on IPFS storage.");
            }

            var downloadName = !string.IsNullOrWhiteSpace(fileName) ? fileName : hash;

            return File(fileBytes, "application/octet-stream", downloadName);
        }
    }
}
