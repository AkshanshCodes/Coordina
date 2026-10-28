using Coordina.Api.Modules.Auth;
using Coordina.Api.Modules.Boards;
using Coordina.Api.Modules.Projects;
using Coordina.Api.Modules.Workspaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Coordina.Api.Tests.Support;

public sealed class ApiTestApplicationFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");
    builder.ConfigureTestServices(services =>
    {
      services.AddInMemoryAuthStoreForTests();
      services.AddInMemoryWorkspaceStoreForTests();
      services.AddInMemoryProjectStoreForTests();
      services.AddInMemoryBoardStoreForTests();
    });
  }
}
