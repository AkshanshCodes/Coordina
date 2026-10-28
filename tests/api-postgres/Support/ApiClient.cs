using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Coordina.Api.Postgres.Tests.Support;

public sealed class ApiClient(HttpClient client)
{
  public async Task<TestUser> RegisterAsync()
  {
    var response = await client.PostAsJsonAsync("/auth/register", new
    {
      name = "Postgres Tester",
      email = $"postgres-{Guid.NewGuid():N}@coordina.test",
      password = "Password123!"
    });

    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
    Assert.NotNull(payload);

    return new TestUser(payload.AccessToken, payload.User.Id);
  }

  public async Task<HttpResponseMessage> SendAsync(
    HttpMethod method,
    string path,
    string accessToken,
    object? body = null)
  {
    using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
    request.Headers.Authorization = new AuthenticationHeaderValue(
      "Bearer",
      accessToken);

    if (body is not null)
    {
      request.Content = JsonContent.Create(body);
    }

    return await client.SendAsync(request);
  }

  public async Task<T> SendForAsync<T>(
    HttpMethod method,
    string path,
    string accessToken,
    object? body = null)
  {
    using var response = await SendAsync(method, path, accessToken, body);
    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<T>();
    Assert.NotNull(payload);

    return payload;
  }

  private sealed record AuthResponse(string AccessToken, UserResponse User);

  private sealed record UserResponse(Guid Id);
}

public sealed record TestUser(string AccessToken, Guid Id);
