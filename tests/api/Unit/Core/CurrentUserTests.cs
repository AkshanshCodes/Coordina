using System.Reflection;
using System.Security.Claims;
using Coordina.Api.Core.Security;
using Microsoft.AspNetCore.Http;

namespace Coordina.Api.Tests.Unit.Core;

public sealed class CurrentUserTests
{
  private static readonly ParameterInfo Parameter = typeof(CurrentUserTests)
    .GetMethod(nameof(Handler), BindingFlags.NonPublic | BindingFlags.Static)!
    .GetParameters()[0];

  [Fact]
  public async Task BindAsync_WithUserIdClaim_ReturnsCurrentUser()
  {
    var userId = Guid.NewGuid();
    var context = new DefaultHttpContext
    {
      User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
        "test"))
    };

    var currentUser = await CurrentUser.BindAsync(context, Parameter);

    Assert.Equal(new CurrentUser(userId), currentUser);
  }

  [Fact]
  public async Task BindAsync_WithoutUserIdClaim_ThrowsConfigurationError()
  {
    var context = new DefaultHttpContext();

    await Assert.ThrowsAsync<InvalidOperationException>(
      async () => await CurrentUser.BindAsync(context, Parameter));
  }

  [Fact]
  public void TryGetUserId_WithNonGuidClaim_ReturnsFalse()
  {
    var principal = new ClaimsPrincipal(new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
      "test"));

    Assert.False(principal.TryGetUserId(out _));
  }

  private static void Handler(CurrentUser user) => _ = user;
}
