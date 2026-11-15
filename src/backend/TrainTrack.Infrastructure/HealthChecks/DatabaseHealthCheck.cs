using Microsoft.Extensions.Diagnostics.HealthChecks;
using TrainTrack.Infrastructure.Persistence;

namespace TrainTrack.Infrastructure.HealthChecks;

/// <summary>Sprawdza, czy da się połączyć z PostgreSQL. Przy błędzie zwraca tylko status; szczegóły trafiają do logów.</summary>
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public const string Name = "database";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(context.Registration.FailureStatus);
    }
}
