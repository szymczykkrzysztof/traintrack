namespace TrainTrack.Infrastructure.HealthChecks;

public static class HealthCheckTags
{
    /// <summary>Sprawdzenia zależności zewnętrznych, uruchamiane przez <c>/health/ready</c>.</summary>
    public const string Ready = "ready";
}
