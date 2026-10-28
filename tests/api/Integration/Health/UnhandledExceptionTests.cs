using System.Net;
using System.Net.Http.Json;
using Coordina.Api.Core.Modules;
using Coordina.Api.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coordina.Api.Tests.Integration.Health;

public sealed class UnhandledExceptionTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  private const string SecretMessage = "database password leaked";

  [Fact]
  public async Task UnhandledException_ReturnsProblemDetailsWithoutInternals()
  {
    using var client = Factory
      .WithWebHostBuilder(builder => builder.ConfigureTestServices(
        services => services.AddSingleton<IModule>(new ThrowingModule())))
      .CreateClient();

    var response = await client.GetAsync("/__tests/throw");
    var body = await response.Content.ReadAsStringAsync();

    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    Assert.DoesNotContain(SecretMessage, body, StringComparison.Ordinal);

    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.NotNull(problem);
    Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
    Assert.True(problem.Extensions.ContainsKey("traceId"));
  }

  private sealed class ThrowingModule : IModule
  {
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
      endpoints
        .MapGet("/__tests/throw", IResult () => throw new InvalidOperationException(SecretMessage))
        .AllowAnonymous();
  }
}
