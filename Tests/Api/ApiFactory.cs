using System.Net.Http.Headers;
using MathChain.API.Controllers;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;
using MathChain.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Tests.Api
{
    public class ApiFactory : WebApplicationFactory<UserController>
    {
        public InMemoryUserGateway Users { get; } = new();
        public InMemoryClassRoomGateway ClassRooms { get; } = new();

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__MathchainDB", "Host=localhost;Database=tests;Username=tests;Password=tests");
            Environment.SetEnvironmentVariable("Jwt__Key", "integration-tests-key-with-at-least-32-bytes");
        }

        public HttpClient CreateClientAs(string walletAddress, UserRole role)
        {
            var token = Services.GetRequiredService<IAccessTokenService>()
                .CreateToken(new User { WalletAddress = walletAddress, Role = role });

            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IUserGateway>(Users);
                services.AddSingleton<IClassRoomGateway>(ClassRooms);
            });
        }
    }
}
