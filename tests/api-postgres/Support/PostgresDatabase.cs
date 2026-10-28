using Coordina.Api.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Coordina.Api.Postgres.Tests.Support;

public sealed class PostgresDatabase : IAsyncDisposable
{
  private readonly PostgresContainerFixture _fixture;

  internal PostgresDatabase(PostgresContainerFixture fixture, string name)
  {
    _fixture = fixture;
    Name = name;
    ConnectionString = fixture.ConnectionStringFor(name);
  }

  public string Name { get; }

  public string ConnectionString { get; }

  public CoordinaDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<CoordinaDbContext>()
      .UseNpgsql(ConnectionString)
      .Options;

    return new CoordinaDbContext(options);
  }

  public async Task<NpgsqlConnection> OpenConnectionAsync()
  {
    var connection = new NpgsqlConnection(ConnectionString);
    await connection.OpenAsync();
    return connection;
  }

  public async Task<T?> ScalarAsync<T>(string sql)
  {
    await using var connection = await OpenConnectionAsync();
    await using var command = new NpgsqlCommand(sql, connection);
    var value = await command.ExecuteScalarAsync();

    return value is null or DBNull ? default : (T)value;
  }

  public async Task ExecuteAsync(string sql)
  {
    await using var connection = await OpenConnectionAsync();
    await using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
  }

  public async ValueTask DisposeAsync()
  {
    NpgsqlConnection.ClearAllPools();
    await _fixture.ExecuteAdminAsync(
      $"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)");
  }
}
