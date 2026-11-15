using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainTrack.Api.IntegrationTests.Infrastructure;
using TrainTrack.Infrastructure.Persistence;

namespace TrainTrack.Api.IntegrationTests;

[Collection(PostgreSqlGroup.Name)]
public class DatabaseMigrationTests(PostgreSqlFixture postgres)
{
    [Fact]
    public async Task Migrations_apply_to_a_fresh_postgresql_database()
    {
        using var factory = new TrainTrackApiFactory(postgres.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var applied = await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        applied.ShouldContain(migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }
}
