using MathChain.Domain.Gateways;
using MathChain.Gateway.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Gateway.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayLayer(this IServiceCollection services)
        {
            services.AddScoped<ICurriculumGateway, CurriculumRepository>();
            services.AddScoped<IClassRoomGateway, ClassRoomRepository>();
            services.AddScoped<IAssignmentGateway, AssignmentRepository>();
            services.AddScoped<ISubmissionGateway, SubmissionRepository>();
            services.AddScoped<IGradeGateway, GradeRepository>();
            services.AddScoped<IClassContentGateway, ClassContentRepository>();
            services.AddScoped<IUserGateway, UserRepository>();
            services.AddScoped<IExternalLoginGateway, ExternalLoginRepository>();
            services.AddScoped<ICertificateGateway, CertificateRepository>();
            services.AddScoped<IPracticeExerciseGateway, PracticeExerciseRepository>();
            services.AddScoped<ISolutionPurchaseGateway, SolutionPurchaseRepository>();
            services.AddScoped<IContentBlockGateway, ContentBlockRepository>();
            services.AddScoped<IChainIndexGateway, ChainIndexRepository>();

            return services;
        }
    }
}
