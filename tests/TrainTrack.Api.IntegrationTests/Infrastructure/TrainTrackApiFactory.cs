using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TrainTrack.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Uruchamia API w pamięci z jawnie podanym connection stringiem.
/// Środowisko "Testing" sprawia, że user-secrets developera nie wpływają na wynik testów.
/// </summary>
public sealed class TrainTrackApiFactory(string? connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
            }));
    }
}
