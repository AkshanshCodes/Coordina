using Coordina.Api.Core.Results;
using Coordina.Api.Shared.Enums;

namespace Coordina.Api.Shared.Validation;

/// <summary>
/// Collects field errors with the wording used across the API, then turns them
/// into a validation <see cref="Error"/>.
/// </summary>
public sealed class ValidationErrors
{
  private readonly Dictionary<string, List<string>> errors = new(StringComparer.Ordinal);

  public bool HasErrors => errors.Count > 0;

  public bool Contains(string field) => errors.ContainsKey(field);

  public ValidationErrors Add(string field, string message)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(field);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    if (!errors.TryGetValue(field, out var messages))
    {
      messages = [];
      errors[field] = messages;
    }

    messages.Add(message);
    return this;
  }

  public ValidationErrors AddIf(bool condition, string field, string message) =>
    condition ? Add(field, message) : this;

  /// <summary>
  /// Requires a non-blank value whose trimmed length fits the given bounds.
  /// Reports at most one message for the field.
  /// </summary>
  public ValidationErrors Required(
    string field,
    string? value,
    string label,
    int? minLength = null,
    int? maxLength = null)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return Add(field, $"{label} is required.");
    }

    var length = value.Trim().Length;

    if (length < minLength)
    {
      return Add(field, $"{label} must be at least {minLength} characters.");
    }

    return length > maxLength
      ? Add(field, $"{label} must be {maxLength} characters or fewer.")
      : this;
  }

  /// <summary>Limits the trimmed length of an optional value.</summary>
  public ValidationErrors MaxLength(
    string field,
    string? value,
    string label,
    int maxLength) =>
    value?.Trim().Length > maxLength
      ? Add(field, $"{label} must be {maxLength} characters or fewer.")
      : this;

  /// <summary>
  /// Parses an optional enum code. Blank input is accepted and yields
  /// <c>null</c>; an unknown code reports the list of accepted codes.
  /// </summary>
  public ValidationErrors Code<TEnum>(
    string field,
    string? value,
    string label,
    out TEnum? parsed)
    where TEnum : struct, Enum
  {
    parsed = null;

    if (string.IsNullOrWhiteSpace(value))
    {
      return this;
    }

    if (EnumCode.TryParse<TEnum>(value, out var code))
    {
      parsed = code;
      return this;
    }

    return Add(field, $"{label} must be {EnumCode.Describe<TEnum>()}.");
  }

  /// <summary>Same as <see cref="Code{TEnum}"/>, but blank input is an error.</summary>
  public ValidationErrors RequiredCode<TEnum>(
    string field,
    string? value,
    string label,
    out TEnum? parsed)
    where TEnum : struct, Enum
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      parsed = null;
      return Add(field, $"{label} is required.");
    }

    return Code(field, value, label, out parsed);
  }

  public IReadOnlyDictionary<string, string[]> ToDictionary() =>
    errors.ToDictionary(
      entry => entry.Key,
      entry => entry.Value.ToArray(),
      StringComparer.Ordinal);

  public Error ToError() => Error.Validation(ToDictionary());
}
