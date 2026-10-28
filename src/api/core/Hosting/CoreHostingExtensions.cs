using Coordina.Api.Core.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scalar.AspNetCore;

namespace Coordina.Api.Core.Hosting;

/// <summary>Cross-cutting services and middleware shared by every module.</summary>
public static class CoreHostingExtensions
{
  public const string TestingEnvironment = "Testing";
  private const string WebAppCorsPolicy = "WebApp";

  private static readonly string[] WebAppOrigins =
  [
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5174"
  ];

  public static IServiceCollection AddCore(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
  {
    ArgumentNullException.ThrowIfNull(environment);

    services.TryAddSingleton(TimeProvider.System);
    services.AddProblemDetails();
    services.AddCors(options =>
    {
      options.AddPolicy(WebAppCorsPolicy, policy =>
      {
        policy
          .WithOrigins(WebAppOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod();
      });
    });

    if (!environment.IsEnvironment(TestingEnvironment))
    {
      services.AddPostgres(configuration);
    }

    services.AddAuthorization();
    services.AddOpenApi();

    return services;
  }

  public static WebApplication UseCore(this WebApplication app)
  {
    ArgumentNullException.ThrowIfNull(app);

    // Development keeps the detailed exception page; elsewhere unhandled
    // exceptions become a ProblemDetails 500 with a trace id and no internals.
    if (!app.Environment.IsDevelopment())
    {
      app.UseExceptionHandler();
    }

    app.UseCors(WebAppCorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference("/api-docs", options =>
    {
      options.Title = "Coordina API";
    })
    .AllowAnonymous();

    return app;
  }
}
