using System.Security.Claims;
using Coordina.Api.Core.Http;
using Coordina.Api.Modules.Auth.Application;
using Coordina.Api.Modules.Auth.Contracts;

namespace Coordina.Api.Modules.Auth;

public static class AuthEndpoints
{
  public static IEndpointRouteBuilder MapAuthEndpoints(
    this IEndpointRouteBuilder app)
  {
    var group = app.MapApiGroup("/auth", "Auth");

    group.MapPost("/register", Register)
      .AllowAnonymous()
      .WithSummary("Create a workspace account")
      .WithDescription("Creates a user account and returns an access token.");
    group.MapPost("/login", Login)
      .AllowAnonymous()
      .WithSummary("Sign in")
      .WithDescription("Authenticates a user and returns an access token.");
    group.MapGet("/me", GetCurrentUser)
      .WithSummary("Get current user")
      .WithDescription("Returns the user attached to the bearer token.");

    return app;
  }

  private static async Task<IResult> Register(
    RegisterRequest request,
    IAuthService authService,
    CancellationToken cancellationToken)
  {
    var result = await authService.RegisterAsync(request, cancellationToken);

    return result.ToCreatedResult(_ => "/auth/me");
  }

  private static async Task<IResult> Login(
    LoginRequest request,
    IAuthService authService,
    CancellationToken cancellationToken)
  {
    var result = await authService.LoginAsync(request, cancellationToken);

    return result.ToOkResult();
  }

  private static IResult GetCurrentUser(ClaimsPrincipal user)
  {
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(ClaimTypes.Email);
    var name = user.FindFirstValue(ClaimTypes.Name);

    if (string.IsNullOrWhiteSpace(userId)
      || string.IsNullOrWhiteSpace(email)
      || string.IsNullOrWhiteSpace(name))
    {
      return Results.Unauthorized();
    }

    return Results.Ok(new CurrentUserResponse(userId, email, name));
  }
}
