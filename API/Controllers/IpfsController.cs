using MathChain.API.Services;
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
        private readonly string _storagePath;

        public IpfsController(IWebHostEnvironment env)
        {
            _storagePath = Path.Combine(env.ContentRootPath, "Storage", "ipfs");
            if(!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            var file = request?.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();

                string ipfsHash = IpfsHashHelper.ComputeCid(fileBytes);
                string destinationPath = Path.Combine(_storagePath, ipfsHash);

                if(!System.IO.File.Exists(destinationPath))
                {
                    await System.IO.File.WriteAllBytesAsync(destinationPath, fileBytes);
                }

                return Ok(new { ipfsHash });
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Internal storage error: {ex.Message}");
            }
        }

        [HttpGet("download/{hash}")]
        [HttpGet("{hash}")]
        public async Task<IActionResult> Download(string hash, [FromQuery] string? fileName)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return BadRequest("Hash is required.");
            }

            string filePath = Path.Combine(_storagePath, hash);

            if(!System.IO.File.Exists(filePath))
            {
                return NotFound($"Content with hash {hash} not found in local IPFS node.");
            }

            var fileStream = System.IO.File.OpenRead(filePath);
            var safeFileName = string.IsNullOrWhiteSpace(fileName) ? $"{hash}.bin" : fileName;

            return File(fileStream, "application/octet-stream", safeFileName);
        }
    }
}
