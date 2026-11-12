using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassMediaController : ApiControllerBase
    {
        private readonly IClassContentContext _contentContext;
        public ClassMediaController(IClassContentContext contentContext)
        {
            _contentContext = contentContext;
        }

        [HttpGet("{classRoomId}/media")]
        public async Task<IActionResult> GetClassMedia(Guid classRoomId)
        {
            var media = await _contentContext.GetMediaAsync(classRoomId);

            return Success(media);
        }

        [HttpPost("{classRoomId}/media")]
        public async Task<IActionResult> AddClassMedia(Guid classRoomId, [FromBody] CreateClassMediaRequest request)
        {
            var media = await _contentContext.AddMediaAsync(classRoomId, request);

            return Success(media, "Class media uploaded successfully.", StatusCodes.Status201Created);
        }

        [HttpDelete("media/{mediaId:guid}")]
        public async Task<IActionResult> DeleteClassMedia(Guid mediaId)
        {
            await _contentContext.DeleteMediaAsync(mediaId);

            return SuccessMessage("Media deleted successfully.");
        }
    }
}
