namespace Coordina.Api.Tests.Support;

/// <summary>
/// Base class for endpoint tests: one in-memory API per test class and an
/// <see cref="ApiTestClient"/> to drive it.
/// </summary>
public abstract class ApiTest(ApiTestApplicationFactory factory)
  : IClassFixture<ApiTestApplicationFactory>
{
  protected ApiTestApplicationFactory Factory { get; } = factory;

  protected ApiTestClient Api { get; } = new(factory.CreateClient());
}
