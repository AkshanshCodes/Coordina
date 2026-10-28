using Coordina.Api.Infrastructure.Configuration;
using Coordina.Api.Infrastructure.Persistence;
using Coordina.Api.Modules.Auth;
using Coordina.Api.Modules.Boards;
using Coordina.Api.Modules.Health;
using Coordina.Api.Modules.Projects;
using Coordina.Api.Modules.Workspaces;
using Scalar.AspNetCore;

EnvFile.LoadNearest();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddCors(options =>
{
  options.AddPolicy("WebApp", policy =>
  {
    policy
      .WithOrigins(
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5174")
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

if (!builder.Environment.IsEnvironment("Testing"))
{
  builder.Services.AddPostgres(builder.Configuration);
}

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddWorkspacesModule();
builder.Services.AddProjectsModule();
builder.Services.AddBoardsModule();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("WebApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapScalarApiReference("/api-docs", options =>
{
  options.Title = "Coordina API";
});

app.MapGet("/", () => Results.Ok(new
{
  message = "Welcome to the Coordina API."
}))
.WithTags("System")
.WithSummary("API welcome message");

app.MapAuthEndpoints();
app.MapWorkspacesEndpoints();
app.MapProjectsEndpoints();
app.MapBoardEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program;
