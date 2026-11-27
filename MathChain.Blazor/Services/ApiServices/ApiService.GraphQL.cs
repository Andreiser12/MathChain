using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public const string ChainOverviewQuery = """
            {
              stats { certificates activeCertificates revokedCertificates certificatesClaimedBySmartAccounts smartAccounts solutionPurchases solutionRevenue curriculumVersions }
              indexerStatus { enabled chainId startBlock lastIndexedBlock chainHead confirmations confirmedHead updatedAt }
            }
            """;

        public const string ChainCertificateQuery = """
            query ($tokenId: Long!) {
              certificate(tokenId: $tokenId) {
                tokenId holder issuer kind claimedBySmartAccount issuedBlock issuedAt issuedTransaction revoked revokedBy revokedAt revokedTransaction
              }
              indexerStatus { enabled lastIndexedBlock }
            }
            """;

        public string GraphQlEndpoint => $"{_baseUrl}/graphql";

        public async Task<ApiResult<T>> QueryGraphQlAsync<T>(string query, object? variables = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(GraphQlEndpoint, new { query, variables });
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                {
                    return new ApiResult<T> { Error = errors[0].TryGetProperty("message", out var message) ? message.GetString() : "The query failed." };
                }

                if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                {
                    return new ApiResult<T> { Error = "The blockchain index returned no data." };
                }

                return new ApiResult<T> { Data = data.Deserialize<T>(_jsonOptions) };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<T> { Error = ServerUnreachableMessage };
            }
            catch (JsonException)
            {
                return new ApiResult<T> { Error = "The blockchain index returned an unexpected response." };
            }
        }
    }
}
