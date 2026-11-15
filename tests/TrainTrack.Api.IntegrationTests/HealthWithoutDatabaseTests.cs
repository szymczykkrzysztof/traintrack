using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrainTrack.Api.IntegrationTests.Infrastructure;

namespace TrainTrack.Api.IntegrationTests;

/// <summary>API z connection stringiem wskazującym zamknięty port: baza „niedostępna”, bez zatrzymywania kontenera.</summary>
public sealed class HealthWithoutDatabaseTests : IDisposable
{
    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=traintrack;Username=traintrack;Password=secret-that-must-not-leak;Timeout=1";

    private readonly TrainTrackApiFactory _factory = new(UnreachableDatabase);

    public void Dispose() => _factory.Dispose();

    // I2
    [Fact]
    public async Task Ready_returns_503_with_unhealthy_database_when_database_is_unreachable()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var health = await HealthResponse.ReadAsync(response, ct);
        health.Status.ShouldBe("Unhealthy");
        health.Checks.ShouldBe(new Dictionary<string, string> { ["database"] = "Unhealthy" });
    }

    // I3
    [Fact]
    public async Task Ready_does_not_reveal_technical_details_when_database_is_unreachable()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        body.ShouldNotBeNullOrWhiteSpace();
        using var parsed = new HttpResponseMessage { Content = new StringContent(body) };
        (await HealthResponse.ReadAsync(parsed, ct)).TopLevelKeys.ShouldBe(["status", "checks"]);
        foreach (var leak in new[] { "Host=", "Password", "secret-that-must-not-leak", "127.0.0.1", "Npgsql", "Exception", "   at " })
        {
            body.ShouldNotContain(leak, Case.Insensitive);
        }
    }

    // I4
    [Fact]
    public async Task Live_returns_healthy_even_when_database_is_unreachable()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var health = await HealthResponse.ReadAsync(response, ct);
        health.Status.ShouldBe("Healthy");
        health.Checks.ShouldBeEmpty();
    }

    // I5
    [Fact]
    public void Database_check_times_out_after_three_seconds()
    {
        var options = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var database = options.Registrations.ShouldHaveSingleItem();
        database.Name.ShouldBe("database");
        database.Timeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    // I6
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_are_available_without_authentication(string path)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        using var response = await client.GetAsync(path, ct);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    // I7
    [Fact]
    public async Task Ready_response_is_not_cacheable()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", ct);

        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        (cacheControl.NoStore || cacheControl.NoCache).ShouldBeTrue();
    }
}
