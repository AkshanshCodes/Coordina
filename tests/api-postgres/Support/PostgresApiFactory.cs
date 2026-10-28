using Coordina.Api.Core.Hosting;
using Coordina.Api.Core.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coordina.Api.Postgres.Tests.Support;

public sealed class PostgresApiFactory(PostgresDatabase database)
  : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment(CoreHostingExtensions.TestingEnvironment);
    builder.ConfigureTestServices(services =>
    {
      services.AddDbContext<CoordinaDbContext>(options =>
        options.UseNpgsql(database.ConnectionString));
    });
  }
}
