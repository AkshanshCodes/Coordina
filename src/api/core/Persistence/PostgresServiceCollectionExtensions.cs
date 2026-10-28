using Microsoft.EntityFrameworkCore;

namespace Coordina.Api.Core.Persistence;

public static class PostgresServiceCollectionExtensions
{
  public static IServiceCollection AddPostgres(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    var options = PostgresOptions.FromEnvironment(configuration);

    services.AddSingleton(options);
    services.AddDbContext<CoordinaDbContext>(dbContextOptions =>
    {
      dbContextOptions.UseNpgsql(options.ToConnectionString());
    });

    return services;
  }
}
