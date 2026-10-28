using Coordina.Api.Core.Time;

namespace Coordina.Api.Tests.Unit.Core;

public sealed class TimeProviderExtensionsTests
{
  [Fact]
  public void GetUtcToday_UsesUtcCalendarDate()
  {
    var clock = new FixedTimeProvider(
      new DateTimeOffset(2026, 10, 28, 23, 30, 0, TimeSpan.FromHours(-5)));

    Assert.Equal(new DateOnly(2026, 10, 29), clock.GetUtcToday());
  }

  private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
  }
}
