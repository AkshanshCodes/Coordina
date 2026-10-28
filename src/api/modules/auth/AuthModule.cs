using System.Text;
using Coordina.Api.Core.Modules;
using Coordina.Api.Core.Security;
using Coordina.Api.Modules.Auth.Application;
using Coordina.Api.Modules.Auth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Coordina.Api.Modules.Auth;

public sealed class AuthModule : IModule
{
  public void AddServices(
    IServiceCollection services,
    IConfiguration configuration)
  {
    var authOptions = configuration
      .GetSection(AuthOptions.SectionName)
      .Get<AuthOptions>() ?? throw new InvalidOperationException(
        "Auth configuration is missing.");
    authOptions.Validate();

    services.AddSingleton(authOptions);
    services.AddScoped<IUserStore, PostgresUserStore>();
    services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
    services.AddScoped<IAuthService, AuthService>();

    var signingKey = new SymmetricSecurityKey(
      Encoding.UTF8.GetBytes(authOptions.SigningKey));

    services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidIssuer = authOptions.Issuer,
          ValidateAudience = true,
          ValidAudience = authOptions.Audience,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = signingKey,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
          // Endpoints bind CurrentUser from the token, so reject tokens that
          // do not carry a usable user id at authentication time (401).
          OnTokenValidated = context =>
          {
            if (context.Principal?.TryGetUserId(out _) != true)
            {
              context.Fail("Access token has no valid user identifier.");
            }

            return Task.CompletedTask;
          }
        };
      });
  }

  public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapAuthEndpoints();
}
