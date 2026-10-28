using Coordina.Api.Core.Modules;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Modules.Workspaces.Infrastructure;

namespace Coordina.Api.Modules.Workspaces;

public sealed class WorkspacesModule : IModule
{
  public void AddServices(
    IServiceCollection services,
    IConfiguration configuration)
  {
    services.AddScoped<IWorkspaceStore, PostgresWorkspaceStore>();
    services.AddScoped<IWorkspaceService, WorkspaceService>();
  }

  public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapWorkspacesEndpoints();
}
