using MathChain.Application.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddScoped<ICurriculumContext, CurriculumContext>();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<IClassRoomContext, ClassRoomContext>();
            services.AddScoped<IClassContentContext, ClassContentContext>();
            services.AddScoped<IAssignmentContext, AssignmentContext>();
            services.AddScoped<IDashboardContext, DashboardContext>();
            services.AddScoped<IGradebookContext, GradebookContext>();
            services.AddScoped<IClassStatisticsContext, ClassStatisticsContext>();

            return services;
        }
    }
}
