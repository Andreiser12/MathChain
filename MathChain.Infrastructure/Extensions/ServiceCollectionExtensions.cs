using MathChain.Blockchain;
using MathChain.Blockchain.Services;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Ipfs;
using MathChain.Infrastructure.Wolfram;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services)
        {
            services.AddSingleton<IIpfsStorageService, IpfsStorageService>();
            services.AddHttpClient<IWolframService, WolframService>();

            var blockchainConfig = new BlockchainConfig();
            services.AddSingleton(blockchainConfig);
            services.AddSingleton<IBlockchainService, BlockchainService>();

            return services;
        }
    }
}
