using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TrainTrack.Infrastructure.HealthChecks;

namespace TrainTrack.Api.Health;

public static class HealthEndpoints
{
    /// <summary>
    /// <c>/health/live</c>: proces działa (bez sprawdzeń zależności).
    /// <c>/health/ready</c>: API i zależności otagowane <see cref="HealthCheckTags.Ready"/>.
    /// Oba publiczne; treść zawiera wyłącznie statusy (spec slice'a 0, „Bezpieczeństwo”).
    /// </summary>
    public static IEndpointRouteBuilder MapTrainTrackHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteStatusOnlyAsync,
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = WriteStatusOnlyAsync,
        }).AllowAnonymous();

        return endpoints;
    }

    // Celowo pomija Description, Exception i Data z HealthReportEntry – mogą zawierać connection string lub stack trace.
    private static Task WriteStatusOnlyAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()),
        };

        return JsonSerializer.SerializeAsync(context.Response.Body, body, cancellationToken: context.RequestAborted);
    }
}
