using Testcontainers.PostgreSql;

namespace TrainTrack.Api.IntegrationTests.Infrastructure;

/// <summary>Jeden kontener PostgreSQL na cały przebieg testów (ADR-001: ta sama wersja co w docker-compose.yml).</summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public const string Image = "postgres:18";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image).Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
