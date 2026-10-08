using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassMediaController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassMediaController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet("{classRoomId}/media")]
        public async Task<IActionResult> GetClassMedia(Guid classRoomId)
        {
            var mediaList = await _context.ClassMedia
                .Where(m => m.ClassRoomId == classRoomId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(mediaList);
        }

        [HttpPost("{classRoomId}/media")]
        public async Task<IActionResult> AddClassMedia(Guid classRoomId, [FromBody] CreateClassMediaRequest request)
        {
            var media = new ClassMedia
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                FileName = request.FileName,
                IpfsHash = request.IpfsHash,
                UploaderWallet = request.UploaderWallet,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassMedia.Add(media);
            await _context.SaveChangesAsync();

            return Ok(media);
        }

        [HttpDelete("media/{mediaId:guid}")]
        public async Task<IActionResult> DeleteClassMedia(Guid mediaId)
        {
            var media = await _context.ClassMedia.FindAsync(mediaId);
            if (media == null)
            {
                return NotFound("Media file not found!");
            }

            _context.ClassMedia.Remove(media);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
