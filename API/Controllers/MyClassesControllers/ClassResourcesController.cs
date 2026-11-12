using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassResourcesController : ApiControllerBase
    {
        private readonly IClassContentContext _contentContext;
        public ClassResourcesController(IClassContentContext contentContext)
        {
            _contentContext = contentContext;
        }

        [HttpGet("{classRoomId}/resources")]
        public async Task<IActionResult> GetResources(Guid classRoomId)
        {
            var resources = await _contentContext.GetResourcesAsync(classRoomId);

            return Success(resources);
        }

        [HttpPost("{classRoomId}/resources")]
        public async Task<IActionResult> AddResource(Guid classRoomId, [FromBody] CreateResourceRequest request)
        {
            var resource = await _contentContext.AddResourceAsync(classRoomId, request);

            return Success(resource, "Resource material added successfully.", StatusCodes.Status201Created);
        }

        [HttpDelete("materials/{resourceId:guid}")]
        public async Task<IActionResult> DeleteResource(Guid resourceId)
        {
            await _contentContext.DeleteResourceAsync(resourceId);

            return SuccessMessage("Resource deleted successfully.");
        }
    }
}
