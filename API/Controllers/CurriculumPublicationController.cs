using MathChain.Application.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/curriculum-publication")]
    public class CurriculumPublicationController : ApiControllerBase
    {
        private readonly ICurriculumPublicationContext _publication;

        public CurriculumPublicationController(ICurriculumPublicationContext publication)
        {
            _publication = publication;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            return Success(await _publication.GetStatusAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Publish()
        {
            var status = await _publication.PublishAsync();

            return Success(status, $"Curriculum published as version {status.Published!.Version}.", StatusCodes.Status201Created);
        }

        [AllowAnonymous]
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest()
        {
            return Success(await _publication.GetLatestAsync());
        }
    }
}
