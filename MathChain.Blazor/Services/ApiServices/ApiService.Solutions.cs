using System.Net.Http.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ApiResult<SolutionOfferDto>> GetSolutionOfferAsync(Guid formulaId)
        {
            return await SendForResultAsync<SolutionOfferDto>(() =>
                _httpClient.GetAsync($"{_baseUrl}/solutions/formulas/{formulaId}"));
        }

        public async Task<ApiResult<SolutionQuoteDto>> CreateSolutionQuoteAsync(Guid formulaId, int index, string payerAddress)
        {
            return await SendForResultAsync<SolutionQuoteDto>(() =>
                _httpClient.PostAsJsonAsync($"{_baseUrl}/solutions/formulas/{formulaId}/exercises/{index}/quote", new { PayerAddress = payerAddress }));
        }

        public async Task<ApiResult<SolutionPurchaseDto>> SubmitSolutionPaymentAsync(Guid purchaseId, string signature)
        {
            return await SendForResultAsync<SolutionPurchaseDto>(() =>
                _httpClient.PostAsJsonAsync($"{_baseUrl}/solutions/purchases/{purchaseId}", new { Signature = signature }));
        }

        public async Task<ApiResult<SolutionPurchaseDto>> GetSolutionPurchaseAsync(Guid purchaseId)
        {
            return await SendForResultAsync<SolutionPurchaseDto>(() =>
                _httpClient.GetAsync($"{_baseUrl}/solutions/purchases/{purchaseId}"));
        }

        public async Task<ApiResult<ExerciseSolutionDto>> GetExerciseSolutionAsync(Guid exerciseId)
        {
            return await SendForResultAsync<ExerciseSolutionDto>(() =>
                _httpClient.GetAsync($"{_baseUrl}/solutions/exercises/{exerciseId}"));
        }

        public async Task<ApiResult<SolutionPaymentSetupDto>> GetSolutionSetupAsync()
        {
            return await SendForResultAsync<SolutionPaymentSetupDto>(() =>
                _httpClient.GetAsync($"{_baseUrl}/solutions/setup"));
        }

        public async Task<ApiResult<SolutionPriceChangeDto>> BuildSolutionPriceChangeAsync(string difficulty, decimal price)
        {
            return await SendForResultAsync<SolutionPriceChangeDto>(() =>
                _httpClient.PostAsJsonAsync($"{_baseUrl}/solutions/setup/prices", new { Difficulty = difficulty, Price = price }));
        }
    }
}
