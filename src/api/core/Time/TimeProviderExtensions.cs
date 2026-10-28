namespace Coordina.Api.Core.Time;

public static class TimeProviderExtensions
{
  /// <summary>Current calendar date in UTC, used for due-date rules.</summary>
  public static DateOnly GetUtcToday(this TimeProvider clock)
  {
    ArgumentNullException.ThrowIfNull(clock);

    return DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
  }
}
