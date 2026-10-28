using System.Net;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class ApiEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public async Task Root_ReturnsWelcomeMessage()
  {
    var response = await Api.Http.GetAsync("/");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var payload = await response.Content.ReadFromJsonAsync<WelcomeResponse>();
    Assert.NotNull(payload);
    Assert.Equal("Welcome to the Coordina API.", payload.Message);
  }

  [Fact]
  public async Task Health_ReturnsServiceStatus()
  {
    var response = await Api.Http.GetAsync("/health");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
    Assert.NotNull(payload);
    Assert.Equal("ok", payload.Status);
    Assert.Equal("Coordina API", payload.Service);
  }

  private sealed record WelcomeResponse(string Message);

  private sealed record HealthResponse(string Status, string Service, DateTime Time);
}
