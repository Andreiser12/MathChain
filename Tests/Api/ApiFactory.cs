using MathChain.API.Controllers;
using MathChain.Domain.Gateways;
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

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__MathchainDB", "Host=localhost;Database=tests;Username=tests;Password=tests");
            Environment.SetEnvironmentVariable("Jwt__Key", "integration-tests-key-with-at-least-32-bytes");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IUserGateway>(Users);
            });
        }
    }
}
