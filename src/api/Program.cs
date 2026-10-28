using Coordina.Api.Core.Configuration;
using Coordina.Api.Core.Hosting;
using Coordina.Api.Core.Modules;
using Coordina.Api.Modules.Auth;
using Coordina.Api.Modules.Boards;
using Coordina.Api.Modules.Health;
using Coordina.Api.Modules.Projects;
using Coordina.Api.Modules.Workspaces;

EnvFile.LoadNearest();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddCore(builder.Configuration, builder.Environment);
builder.Services.AddModules(
  builder.Configuration,
  new HealthModule(),
  new AuthModule(),
  new WorkspacesModule(),
  new ProjectsModule(),
  new BoardsModule());

var app = builder.Build();

app.UseCore();
app.MapModules();

app.Run();

public partial class Program;
