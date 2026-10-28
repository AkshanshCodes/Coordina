using Coordina.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Coordina.Api.Postgres.Tests.Support;

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresContainerFixture>
{
  public const string Name = "postgres";
}

public sealed class PostgresContainerFixture : IAsyncLifetime
{
  private const string TemplateDatabase = "coordina_template";

  private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
    .Build();

  public async Task InitializeAsync()
  {
    try
    {
      await _container.StartAsync();
    }
    catch (Exception exception)
    {
      throw new InvalidOperationException(
        $"PostgreSQL integration tests require Docker: {exception.Message}",
        exception);
    }

    await ExecuteAdminAsync($"CREATE DATABASE \"{TemplateDatabase}\"");

    var options = new DbContextOptionsBuilder<CoordinaDbContext>()
      .UseNpgsql(ConnectionStringFor(TemplateDatabase))
      .Options;

    await using (var dbContext = new CoordinaDbContext(options))
    {
      await dbContext.Database.MigrateAsync();
    }

    NpgsqlConnection.ClearAllPools();
  }

  public Task DisposeAsync()
  {
    return _container.DisposeAsync().AsTask();
  }

  public Task<PostgresDatabase> CreateMigratedDatabaseAsync()
  {
    return CreateDatabaseAsync($"TEMPLATE \"{TemplateDatabase}\"");
  }

  public Task<PostgresDatabase> CreateEmptyDatabaseAsync()
  {
    return CreateDatabaseAsync(string.Empty);
  }

  internal string ConnectionStringFor(string database)
  {
    return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
    {
      Database = database
    }.ConnectionString;
  }

  internal async Task ExecuteAdminAsync(string sql)
  {
    await using var connection = new NpgsqlConnection(
      ConnectionStringFor("postgres"));
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
  }

  private async Task<PostgresDatabase> CreateDatabaseAsync(string suffix)
  {
    var name = $"coordina_test_{Guid.NewGuid():N}";
    await ExecuteAdminAsync($"CREATE DATABASE \"{name}\" {suffix}");

    return new PostgresDatabase(this, name);
  }
}
