using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassResourcesController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassResourcesController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet("{classRoomId}/materials")]
        public async Task<IActionResult> GetResources(Guid classRoomId)
        {
            var materials = await _context.ClassMaterials
                .Where(m => m.ClassRoomId == classRoomId)
                .OrderBy(m => m.WeekNumber)
                .ThenBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(materials);
        }

        [HttpPost("{classRoomId}/materials")]
        public async Task<IActionResult> AddResource(Guid classRoomId, [FromBody] CreateMaterialRequest request)
        {
            var classRoom = await _context.ClassRooms.FindAsync(classRoomId);
            if (classRoom == null)
            {
                return NotFound("Class not found!");
            }

            var material = new ClassResource
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                WeekNumber = request.WeekNumber,
                Title = request.Title,
                Description = request.Description,
                FileName = request.FileName,
                IpfsHash = request.IpfsHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassMaterials.Add(material);
            await _context.SaveChangesAsync();

            return Ok(material);
        }

        [HttpDelete("materials/{materialId:guid}")]
        public async Task<IActionResult> DeleteResource(Guid materialId)
        {
            var material = await _context.ClassMaterials.FindAsync(materialId);
            if (material == null)
            {
                return NotFound("Material not found!");
            }

            _context.ClassMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
