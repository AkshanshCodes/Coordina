using Coordina.Api.Core.Modules;

namespace Coordina.Api.Modules.Health;

public sealed class HealthModule : IModule
{
  public void AddServices(IServiceCollection services, IConfiguration configuration)
  {
  }

  public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapHealthEndpoints();
}
