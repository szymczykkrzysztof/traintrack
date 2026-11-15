using System.Net;
using TrainTrack.Api.IntegrationTests.Infrastructure;

namespace TrainTrack.Api.IntegrationTests;

[Collection(PostgreSqlGroup.Name)]
public class HealthReadinessTests(PostgreSqlFixture postgres)
{
    // I1
    [Fact]
    public async Task Ready_returns_healthy_api_and_database_when_database_is_available()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new TrainTrackApiFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var health = await HealthResponse.ReadAsync(response, ct);
        health.TopLevelKeys.ShouldBe(["status", "checks"]);
        health.Status.ShouldBe("Healthy");
        health.Checks.ShouldBe(new Dictionary<string, string> { ["database"] = "Healthy" });
    }
}
