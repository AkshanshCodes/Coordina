namespace Coordina.Api.Core.Modules;

public static class ModuleExtensions
{
  public static IServiceCollection AddModules(
    this IServiceCollection services,
    IConfiguration configuration,
    params IModule[] modules)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(modules);

    foreach (var module in modules)
    {
      module.AddServices(services, configuration);
      services.AddSingleton(module);
    }

    return services;
  }

  public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    foreach (var module in endpoints.ServiceProvider.GetServices<IModule>())
    {
      module.MapEndpoints(endpoints);
    }

    return endpoints;
  }
}
