using System.Reflection;

namespace Coordina.Api.Core.Security;

/// <summary>
/// Authenticated caller, bound directly as an endpoint parameter. Token
/// validation guarantees a parsable user id, so endpoints protected by
/// <c>RequireAuthorization()</c> can take a <see cref="CurrentUser"/> instead of
/// parsing claims themselves.
/// </summary>
public sealed record CurrentUser(Guid Id)
{
  public static ValueTask<CurrentUser?> BindAsync(
    HttpContext context,
    ParameterInfo parameter)
  {
    ArgumentNullException.ThrowIfNull(context);

    if (!context.User.TryGetUserId(out var userId))
    {
      throw new InvalidOperationException(
        $"Endpoint parameter '{parameter?.Name}' requires an authenticated user. "
        + "Add RequireAuthorization() to the endpoint or its group.");
    }

    return ValueTask.FromResult<CurrentUser?>(new CurrentUser(userId));
  }
}
