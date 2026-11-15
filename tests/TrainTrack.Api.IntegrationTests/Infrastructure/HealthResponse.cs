using System.Text.Json;

namespace TrainTrack.Api.IntegrationTests.Infrastructure;

/// <summary>Odczyt kontraktu health checks: <c>{ status, checks: { nazwa: status } }</c>.</summary>
public sealed record HealthResponse(string Status, IReadOnlyDictionary<string, string> Checks, IReadOnlyList<string> TopLevelKeys)
{
    public static async Task<HealthResponse> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;

        return new HealthResponse(
            root.GetProperty("status").GetString()!,
            root.GetProperty("checks").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!),
            root.EnumerateObject().Select(p => p.Name).ToList());
    }
}
