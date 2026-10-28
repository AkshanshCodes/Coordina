using Coordina.Api.Core.Modules;
using Coordina.Api.Modules.Projects.Application;
using Coordina.Api.Modules.Projects.Infrastructure;

namespace Coordina.Api.Modules.Projects;

public sealed class ProjectsModule : IModule
{
  public void AddServices(
    IServiceCollection services,
    IConfiguration configuration)
  {
    services.AddScoped<IProjectStore, PostgresProjectStore>();
    services.AddScoped<IProjectService, ProjectService>();
    services.AddScoped<IProjectAccessGuard, ProjectAccessGuard>();
  }

  public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapProjectsEndpoints();
}
