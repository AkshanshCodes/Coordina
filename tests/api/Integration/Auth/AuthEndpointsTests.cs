using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Coordina.Api.Modules.Auth;
using Coordina.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Coordina.Api.Tests;

public sealed class AuthEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public async Task Register_ReturnsTokenAndCurrentUser()
  {
    var email = ApiTestClient.UniqueEmail("ada");

    var response = await Api.Http.PostAsJsonAsync("/auth/register", new
    {
      name = "Ada Lovelace",
      email,
      password = "Password123!"
    });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
    Assert.NotNull(payload);
    Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
    Assert.Equal(email, payload.User.Email);
    Assert.Equal("Ada Lovelace", payload.User.Name);
  }

  [Fact]
  public async Task Login_WithValidCredentials_ReturnsToken()
  {
    var email = ApiTestClient.UniqueEmail("ada");
    await RegisterAsync(email);

    var response = await Api.Http.PostAsJsonAsync("/auth/login", new
    {
      email,
      password = "Password123!"
    });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
    Assert.NotNull(payload);
    Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
    Assert.Equal(email, payload.User.Email);
  }

  [Fact]
  public async Task Me_WithBearerToken_ReturnsCurrentUser()
  {
    var email = ApiTestClient.UniqueEmail("ada");
    var auth = await RegisterAsync(email);

    using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
    request.Headers.Authorization = new AuthenticationHeaderValue(
      "Bearer",
      auth.AccessToken);

    var response = await Api.Http.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
    Assert.NotNull(payload);
    Assert.Equal(auth.User.Id, payload.Id);
    Assert.Equal(email, payload.Email);
  }

  [Fact]
  public async Task Me_WithoutBearerToken_ReturnsUnauthorized()
  {
    var response = await Api.Http.GetAsync("/auth/me");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task ProtectedEndpoint_WithTokenWithoutUserId_ReturnsUnauthorized()
  {
    var options = Factory.Services.GetRequiredService<AuthOptions>();
    var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
      issuer: options.Issuer,
      audience: options.Audience,
      claims: [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
      expires: DateTime.UtcNow.AddMinutes(5),
      signingCredentials: new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        SecurityAlgorithms.HmacSha256)));
    using var request = new HttpRequestMessage(HttpMethod.Get, "/workspaces");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await Api.Http.SendAsync(request);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Register_WithDuplicateEmail_ReturnsConflict()
  {
    var email = ApiTestClient.UniqueEmail("ada");
    await RegisterAsync(email);

    var response = await Api.Http.PostAsJsonAsync("/auth/register", new
    {
      name = "Ada Lovelace",
      email,
      password = "Password123!"
    });

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
  }

  [Fact]
  public async Task Register_WithInvalidPayload_ReturnsValidationProblem()
  {
    var response = await Api.Http.PostAsJsonAsync("/auth/register", new
    {
      name = "",
      email = "not-an-email",
      password = "short"
    });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  private async Task<AuthResponse> RegisterAsync(string email)
  {
    var response = await Api.Http.PostAsJsonAsync("/auth/register", new
    {
      name = "Ada Lovelace",
      email,
      password = "Password123!"
    });

    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();

    Assert.NotNull(payload);
    return payload;
  }
}
