using Npgsql;

namespace Coordina.Api.Core.Persistence;

public sealed class PostgresOptions
{
  public string Host { get; init; } = string.Empty;

  public int Port { get; init; }

  public string Database { get; init; } = string.Empty;

  public string Username { get; init; } = string.Empty;

  public string Password { get; init; } = string.Empty;

  public static PostgresOptions FromEnvironment(IConfiguration configuration)
  {
    return new PostgresOptions
    {
      Host = configuration["POSTGRES_HOST"] ?? "localhost",
      Port = int.TryParse(configuration["POSTGRES_PORT"], out var port)
        ? port
        : 5432,
      Database = Required(configuration, "POSTGRES_DB"),
      Username = Required(configuration, "POSTGRES_USER"),
      Password = Required(configuration, "POSTGRES_PASSWORD")
    };
  }

  public string ToConnectionString()
  {
    var builder = new NpgsqlConnectionStringBuilder
    {
      Host = Host,
      Port = Port,
      Database = Database,
      Username = Username,
      Password = Password,
      IncludeErrorDetail = false,
      Pooling = true
    };

    return builder.ConnectionString;
  }

  private static string Required(IConfiguration configuration, string key)
  {
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidOperationException(
        $"Missing required environment variable '{key}'.");
    }

    return value;
  }
}
