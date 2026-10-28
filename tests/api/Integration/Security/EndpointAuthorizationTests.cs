using Coordina.Api.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Coordina.Api.Tests.Integration.Security;

/// <summary>
/// Every endpoint must state who may call it. Module groups are authenticated
/// by default (MapApiGroup); public endpoints opt out explicitly and must be
/// listed here, so a new anonymous route is visible in review.
/// </summary>
public sealed class EndpointAuthorizationTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  private static readonly string[] AnonymousEndpoints =
  [
    "GET /",
    "GET /health",
    "POST /auth/register",
    "POST /auth/login",
    "GET /api-docs/*",
    "GET /openapi/*"
  ];

  // Documentation routes (OpenAPI document, Scalar UI and its assets) are
  // grouped so framework upgrades that add asset routes do not break the list.
  private static readonly string[] DocumentationPrefixes = ["/api-docs/", "/openapi/"];

  [Fact]
  public void EveryEndpoint_DeclaresAnAuthorizationPolicy()
  {
    var undeclared = Endpoints()
      .Where(endpoint => !RequiresAuthorization(endpoint) && !AllowsAnonymous(endpoint))
      .Select(Describe)
      .ToArray();

    Assert.True(
      undeclared.Length == 0,
      "Endpoints without RequireAuthorization() or AllowAnonymous():"
        + Environment.NewLine
        + string.Join(Environment.NewLine, undeclared));
  }

  [Fact]
  public void AnonymousEndpoints_AreExplicitlyAllowed()
  {
    var anonymous = Endpoints()
      .Where(AllowsAnonymous)
      .Select(Describe)
      .Select(GroupDocumentation)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal)
      .ToArray();

    Assert.Equal(AnonymousEndpoints.Order(StringComparer.Ordinal), anonymous);
  }

  private RouteEndpoint[] Endpoints() => Factory.Services
    .GetRequiredService<EndpointDataSource>()
    .Endpoints
    .OfType<RouteEndpoint>()
    .ToArray();

  private static bool RequiresAuthorization(RouteEndpoint endpoint) =>
    endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;

  private static bool AllowsAnonymous(RouteEndpoint endpoint) =>
    endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

  private static string GroupDocumentation(string endpoint)
  {
    var separator = endpoint.IndexOf(' ', StringComparison.Ordinal);
    var path = endpoint[(separator + 1)..];
    var prefix = DocumentationPrefixes.FirstOrDefault(
      candidate => path.StartsWith(candidate, StringComparison.Ordinal));

    return prefix is null ? endpoint : $"{endpoint[..separator]} {prefix}*";
  }

  private static string Describe(RouteEndpoint endpoint)
  {
    var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
      ?? ["ANY"];

    return $"{string.Join(",", methods)} /{endpoint.RoutePattern.RawText?.TrimStart('/')}";
  }
}
