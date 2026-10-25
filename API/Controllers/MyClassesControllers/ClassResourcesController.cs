using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassResourcesController : ControllerBase
    {
        private readonly IClassContentContext _contentContext;
        public ClassResourcesController(IClassContentContext contentContext)
        {
            _contentContext = contentContext;
        }

        [HttpGet("{classRoomId}/resources")]
        public async Task<IActionResult> GetResources(Guid classRoomId)
        {
            var result = await _contentContext.GetResourcesAsync(classRoomId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{classRoomId}/resources")]
        public async Task<IActionResult> AddResource(Guid classRoomId, [FromBody] CreateResourceRequest request)
        {
            var result = await _contentContext.AddResourceAsync(classRoomId, request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("materials/{resourceId:guid}")]
        public async Task<IActionResult> DeleteResource(Guid resourceId)
        {
            var result = await _contentContext.DeleteResourceAsync(resourceId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
