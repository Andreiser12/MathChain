using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ApiResult<CurriculumPublicationStatusDto>> GetCurriculumPublicationAsync()
        {
            return await SendForResultAsync<CurriculumPublicationStatusDto>(() => _httpClient.GetAsync($"{_baseUrl}/curriculum-publication"));
        }

        public async Task<ApiResult<CurriculumPublicationStatusDto>> PublishCurriculumAsync()
        {
            return await SendForResultAsync<CurriculumPublicationStatusDto>(() => _httpClient.PostAsync($"{_baseUrl}/curriculum-publication", null));
        }
    }
}
