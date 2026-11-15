using Microsoft.Extensions.Options;
using TrainTrack.Api.IntegrationTests.Infrastructure;

namespace TrainTrack.Api.IntegrationTests;

public class StartupConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Api_does_not_start_without_database_connection_string(string? connectionString)
    {
        using var factory = new TrainTrackApiFactory(connectionString);

        var exception = Should.Throw<OptionsValidationException>(() => factory.CreateClient());

        exception.Message.ShouldContain("ConnectionString");
    }
}
