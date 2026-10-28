namespace Coordina.Api.Core.Http;

public static class ApiGroupExtensions
{
  /// <summary>
  /// Maps a group of module endpoints. Groups require an authenticated user
  /// by default; public endpoints must opt out with <c>AllowAnonymous()</c>.
  /// </summary>
  public static RouteGroupBuilder MapApiGroup(
    this IEndpointRouteBuilder endpoints,
    string prefix,
    string tag)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    return endpoints
      .MapGroup(prefix)
      .RequireAuthorization()
      .WithTags(tag);
  }
}
