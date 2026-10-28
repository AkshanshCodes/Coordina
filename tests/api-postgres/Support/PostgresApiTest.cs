using System.Diagnostics.CodeAnalysis;

namespace Coordina.Api.Postgres.Tests.Support;

[Collection(PostgresTestGroup.Name)]
[SuppressMessage(
  "Design",
  "CA1001",
  Justification = "xUnit disposes fields through IAsyncLifetime.DisposeAsync.")]
public abstract class PostgresApiTest(PostgresContainerFixture postgres)
  : IAsyncLifetime
{
  private PostgresDatabase? _database;
  private PostgresApiFactory? _factory;
  private ApiClient? _api;

  protected PostgresContainerFixture Postgres { get; } = postgres;

  protected PostgresDatabase Database => _database
    ?? throw new InvalidOperationException("Test database is not initialized.");

  protected PostgresApiFactory Factory => _factory
    ?? throw new InvalidOperationException("Test factory is not initialized.");

  protected ApiClient Api => _api
    ?? throw new InvalidOperationException("Test client is not initialized.");

  public async Task InitializeAsync()
  {
    _database = await Postgres.CreateMigratedDatabaseAsync();
    _factory = new PostgresApiFactory(_database);
    _api = new ApiClient(_factory.CreateClient());
  }

  public async Task DisposeAsync()
  {
    if (_factory is not null)
    {
      await _factory.DisposeAsync();
    }

    if (_database is not null)
    {
      await _database.DisposeAsync();
    }
  }
}
