using System.Collections.Frozen;
using System.Text;

namespace Coordina.Api.Shared.Enums;

/// <summary>
/// Converts enums to and from the UPPER_SNAKE_CASE codes used by the API and
/// the database (<c>InProgress</c> ⇄ <c>IN_PROGRESS</c>). Parsing ignores case
/// and underscores, and rejects numeric or undefined values.
/// </summary>
public static class EnumCode
{
  public static string Format<TEnum>(TEnum value)
    where TEnum : struct, Enum =>
    Codes<TEnum>.ByValue.TryGetValue(value, out var code)
      ? code
      : throw new ArgumentOutOfRangeException(
        nameof(value),
        value,
        $"Undefined {typeof(TEnum).Name} value.");

  public static bool TryParse<TEnum>(string? code, out TEnum value)
    where TEnum : struct, Enum
  {
    if (code is not null
      && Codes<TEnum>.ByKey.TryGetValue(ToKey(code), out value))
    {
      return true;
    }

    value = default;
    return false;
  }

  public static TEnum Parse<TEnum>(string code)
    where TEnum : struct, Enum =>
    TryParse<TEnum>(code, out var value)
      ? value
      : throw new FormatException($"Unknown {typeof(TEnum).Name} code '{code}'.");

  /// <summary>
  /// Lists the accepted codes in declaration order for error messages, for
  /// example <c>LOW, MEDIUM, or HIGH</c>.
  /// </summary>
  public static string Describe<TEnum>()
    where TEnum : struct, Enum => Codes<TEnum>.Description;

  private static string ToKey(string code) =>
    code.Trim().Replace("_", string.Empty, StringComparison.Ordinal)
      .ToUpperInvariant();

  private static string ToCode(string name)
  {
    var builder = new StringBuilder(name.Length + 4);

    for (var index = 0; index < name.Length; index++)
    {
      var character = name[index];

      if (index > 0 && char.IsUpper(character) && !char.IsUpper(name[index - 1]))
      {
        builder.Append('_');
      }

      builder.Append(char.ToUpperInvariant(character));
    }

    return builder.ToString();
  }

  private static class Codes<TEnum>
    where TEnum : struct, Enum
  {
    public static readonly FrozenDictionary<TEnum, string> ByValue = Enum
      .GetValues<TEnum>()
      .Distinct()
      .ToFrozenDictionary(value => value, value => ToCode(value.ToString()));

    public static readonly FrozenDictionary<string, TEnum> ByKey = ByValue
      .ToFrozenDictionary(entry => ToKey(entry.Value), entry => entry.Key);

    public static readonly string Description = JoinAlternatives(Enum
      .GetValues<TEnum>()
      .Distinct()
      .Select(value => ByValue[value])
      .ToArray());
  }

  private static string JoinAlternatives(string[] codes) => codes.Length switch
  {
    0 => string.Empty,
    1 => codes[0],
    2 => $"{codes[0]} or {codes[1]}",
    _ => $"{string.Join(", ", codes[..^1])}, or {codes[^1]}"
  };
}
