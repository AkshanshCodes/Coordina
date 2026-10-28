namespace Coordina.Api.Shared.Text;

public static class TextNormalizer
{
  /// <summary>Trims the value and maps blank input to <c>null</c>.</summary>
  public static string? TrimToNull(string? value)
  {
    var trimmed = value?.Trim();
    return string.IsNullOrEmpty(trimmed) ? null : trimmed;
  }

  /// <summary>
  /// Trims every value, drops blanks and case-insensitive duplicates, and keeps
  /// at most <paramref name="limit"/> entries in their original order.
  /// </summary>
  public static string[] DistinctNonBlank(IEnumerable<string?>? values, int limit)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(limit);

    return (values ?? [])
      .Select(TrimToNull)
      .OfType<string>()
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .Take(limit)
      .ToArray();
  }
}
