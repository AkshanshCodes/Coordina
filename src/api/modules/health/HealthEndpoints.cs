namespace Coordina.Api.Modules.Health;

public static class HealthEndpoints
{
  public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/", () => Results.Ok(new
    {
      message = "Welcome to the Coordina API."
    }))
    .AllowAnonymous()
    .WithTags("System")
    .WithSummary("API welcome message");

    app.MapGet("/health", (TimeProvider clock) => Results.Ok(new
    {
      status = "ok",
      service = "Coordina API",
      time = clock.GetUtcNow().UtcDateTime
    }))
    .AllowAnonymous()
    .WithTags("System")
    .WithSummary("Health check");

    return app;
  }
}
