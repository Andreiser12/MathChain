using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Application.Contexts
{
    public class CurriculumContext : ICurriculumContext
    {
        private readonly ICurriculumGateway _curriculumGateway;
        public CurriculumContext(ICurriculumGateway curriculumGateway)
        {
            _curriculumGateway = curriculumGateway;
        }

        public async Task<Envelope<List<CurriculumChapter>>> GetAllChaptersAsync()
        {
            var chapters = await _curriculumGateway.GetAllAsync();

            return EnvelopeExtensions.Success(chapters, "The chapters were successfully loaded.");
        }

        public async Task<Envelope<CurriculumChapter>> GetChapterBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return EnvelopeExtensions.Failure<CurriculumChapter>("The slug is required.", 400);
            }

            var chapter = await _curriculumGateway.GetBySlugAsync(slug);

            if (chapter == null)
            {
                return EnvelopeExtensions.Failure<CurriculumChapter>($"The chapter with the slug '{slug}' was not found.", 404);
            }

            return EnvelopeExtensions.Success(chapter);
        }

        public async Task<Envelope<CurriculumChapter>> UpdateIpfsHashAsync(string slug, string newIpfsHash)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(newIpfsHash))
            {
                return EnvelopeExtensions.Failure<CurriculumChapter>("The slug and the new IPFS hash are required.", 400);
            }

            var chapter = await _curriculumGateway.GetBySlugAsync(slug);

            if (chapter == null)
            {
                return EnvelopeExtensions.Failure<CurriculumChapter>($"The chapter with the slug '{slug}' was not found.", 404);
            }

            await _curriculumGateway.UpdateIpfsHashAsync(slug, newIpfsHash);

            var updatedChapter = await _curriculumGateway.GetBySlugAsync(slug);

            return EnvelopeExtensions.Success(updatedChapter!, "The IPFS hash was successfully updated.");
        }
    }
}
