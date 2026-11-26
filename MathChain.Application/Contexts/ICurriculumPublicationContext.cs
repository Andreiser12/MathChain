using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface ICurriculumPublicationContext
    {
        Task<CurriculumPublicationStatusDto> GetStatusAsync();
        Task<CurriculumPublicationStatusDto> PublishAsync();
        Task<CurriculumAnchorDto?> GetLatestAsync();
    }
}
