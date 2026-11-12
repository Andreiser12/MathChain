using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace MathChain.Blazor.Services
{
    public class SessionTokenHandler : DelegatingHandler
    {
        private readonly AppSession _session;
        private readonly NavigationManager _navigation;
        private readonly string _apiBaseUrl;

        public SessionTokenHandler(AppSession session, NavigationManager navigation, IConfiguration configuration)
        {
            _session = session;
            _navigation = navigation;
            _apiBaseUrl = (configuration["ApiBaseUrl"] ?? "https://localhost:7146/api").TrimEnd('/') + "/";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _session.Token;
            var isApiRequest = request.RequestUri != null
                && request.RequestUri.AbsoluteUri.StartsWith(_apiBaseUrl, StringComparison.OrdinalIgnoreCase);

            if (isApiRequest && !string.IsNullOrEmpty(token) && request.Headers.Authorization == null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (isApiRequest && !string.IsNullOrEmpty(token) && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _session.LogoutAsync();
                _navigation.NavigateTo("/login");
            }

            return response;
        }
    }
}
