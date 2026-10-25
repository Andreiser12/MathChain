using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassMediaController : ControllerBase
    {
        private readonly IClassContentContext _contentContext;
        public ClassMediaController(IClassContentContext contentContext)
        {
            _contentContext = contentContext;
        }

        [HttpGet("{classRoomId}/media")]
        public async Task<IActionResult> GetClassMedia(Guid classRoomId)
        {
            var result = await _contentContext.GetMediaAsync(classRoomId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{classRoomId}/media")]
        public async Task<IActionResult> AddClassMedia(Guid classRoomId, [FromBody] CreateClassMediaRequest request)
        {
            var result = await _contentContext.AddMediaAsync(classRoomId, request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("media/{mediaId:guid}")]
        public async Task<IActionResult> DeleteClassMedia(Guid mediaId)
        {
            var result = await _contentContext.DeleteMediaAsync(mediaId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
