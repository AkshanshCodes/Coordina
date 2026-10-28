using System.Net;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class OpenApiEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public async Task OpenApi_ReturnsDocument()
  {
    var response = await Api.Http.GetAsync("/openapi/v1.json");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadAsStringAsync();
    Assert.Contains("\"/auth/login\"", body);
    Assert.Contains("\"/auth/register\"", body);
  }

  [Fact]
  public async Task ApiDocs_ReturnsInteractiveReference()
  {
    var response = await Api.Http.GetAsync("/api-docs");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }
}
