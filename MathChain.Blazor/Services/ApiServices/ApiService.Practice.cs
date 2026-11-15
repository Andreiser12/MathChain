using System.Net.Http.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ApiResult<PracticeFormulaDto>> GetPracticeFormulaAsync(Guid formulaId)
        {
            return await SendForResultAsync<PracticeFormulaDto>(() =>
                _httpClient.GetAsync($"{_baseUrl}/practice/formulas/{formulaId}"));
        }

        public async Task<ApiResult<PracticeExerciseDto>> StartPracticeExerciseAsync(Guid formulaId, int index)
        {
            return await SendForResultAsync<PracticeExerciseDto>(() =>
                _httpClient.PostAsync($"{_baseUrl}/practice/formulas/{formulaId}/exercises/{index}", null));
        }

        public async Task<ApiResult<PracticeAnswerResultDto>> SubmitPracticeAnswerAsync(Guid exerciseId, string answer)
        {
            return await SendForResultAsync<PracticeAnswerResultDto>(() =>
                _httpClient.PostAsJsonAsync($"{_baseUrl}/practice/exercises/{exerciseId}/answer", new { Answer = answer }));
        }

        public async Task<ApiResult<ChapterCertificateProgressDto>> GetChapterCertificateProgressAsync(Guid chapterId)
        {
            return await SendForResultAsync<ChapterCertificateProgressDto>(() =>
                _httpClient.GetAsync($"{_baseUrl}/certificates/chapters/{chapterId}/progress"));
        }

        public async Task<ApiResult<MyCertificateDto>> ClaimChapterCertificateAsync(Guid chapterId)
        {
            return await SendForResultAsync<MyCertificateDto>(() =>
                _httpClient.PostAsync($"{_baseUrl}/certificates/chapters/{chapterId}", null));
        }
    }
}
