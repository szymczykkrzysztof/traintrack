using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TrainTrack.Infrastructure.HealthChecks;
using TrainTrack.Infrastructure.Persistence;

namespace TrainTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ValidateOnStart: brak connection stringa zatrzymuje start aplikacji, zamiast wywrócić pierwsze zapytanie do bazy.
        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Database") ?? string.Empty)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                DatabaseHealthCheck.Name,
                failureStatus: HealthStatus.Unhealthy,
                tags: [HealthCheckTags.Ready],
                timeout: DatabaseHealthCheck.Timeout);

        return services;
    }
}
