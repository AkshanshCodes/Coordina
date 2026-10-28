using System.Security.Claims;

namespace Coordina.Api.Core.Security;

public static class ClaimsPrincipalExtensions
{
  public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
  {
    ArgumentNullException.ThrowIfNull(principal);

    return Guid.TryParse(
      principal.FindFirstValue(ClaimTypes.NameIdentifier),
      out userId);
  }
}
