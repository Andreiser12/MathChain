using MathChain.Blockchain.Accounts;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Common;
using MathChain.Blockchain.Solutions;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Ipfs;
using MathChain.Infrastructure.Solutions;
using MathChain.Infrastructure.Wolfram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IIpfsStorageService, IpfsStorageService>();
            services.AddScoped<IpfsImporter>();
            services.AddHttpClient<IWolframService, WolframService>();

            services.AddSingleton(configuration.GetSection("Blockchain").Get<BlockchainSettings>() ?? new BlockchainSettings());
            services.AddSingleton<PlatformTransactionLock>();
            services.AddSingleton<ICertificateRegistry, CertificateRegistry>();
            services.AddSingleton<ISolutionPaymentRegistry, SolutionPaymentRegistry>();
            services.AddSingleton<ISmartAccountService, SmartAccountService>();
            services.AddSingleton<ISolutionCatalog, EmbeddedSolutionCatalog>();

            return services;
        }
    }
}
