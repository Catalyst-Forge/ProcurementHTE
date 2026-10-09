using ProcurementHTE.Web.Utils;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Infrastructure;

namespace ProcurementHTE.Web.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        return services.AddInfrastructure(configuration);
    }

    public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        // Lets the soft delete interceptor record who deleted a row.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();

        return services;
    }
}
