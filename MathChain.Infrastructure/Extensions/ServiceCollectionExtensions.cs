using MathChain.Blockchain.Certificates;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Ipfs;
using MathChain.Infrastructure.Wolfram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IIpfsStorageService, IpfsStorageService>();
            services.AddHttpClient<IWolframService, WolframService>();

            services.AddSingleton(configuration.GetSection("Blockchain").Get<BlockchainSettings>() ?? new BlockchainSettings());
            services.AddSingleton<ICertificateRegistry, CertificateRegistry>();

            return services;
        }
    }
}
