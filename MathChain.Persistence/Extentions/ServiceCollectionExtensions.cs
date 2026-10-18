using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Persistence.Extentions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<MathChainDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            return services;
        }
    }
}
