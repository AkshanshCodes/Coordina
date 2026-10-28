namespace Coordina.Api.Core.Modules;

/// <summary>
/// Entry point of a feature module. A module registers its own services and
/// endpoints; the host only lists modules and never wires their internals.
/// </summary>
public interface IModule
{
  void AddServices(IServiceCollection services, IConfiguration configuration);

  void MapEndpoints(IEndpointRouteBuilder endpoints);
}
