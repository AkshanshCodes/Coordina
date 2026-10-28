namespace Coordina.Api.Core.Results;

/// <summary>
/// Expected failure of a use case. Modules return it instead of throwing so
/// endpoints can translate it into a consistent HTTP response.
/// </summary>
public sealed class Error
{
  private static readonly IReadOnlyDictionary<string, string[]> NoFields =
    new Dictionary<string, string[]>();

  private Error(
    ErrorKind kind,
    string? message,
    IReadOnlyDictionary<string, string[]>? fields = null)
  {
    Kind = kind;
    Message = message;
    Fields = fields ?? NoFields;
  }

  public ErrorKind Kind { get; }

  public string? Message { get; }

  /// <summary>Field-level messages, only populated for validation errors.</summary>
  public IReadOnlyDictionary<string, string[]> Fields { get; }

  public static Error Validation(IReadOnlyDictionary<string, string[]> fields)
  {
    ArgumentNullException.ThrowIfNull(fields);

    return new Error(ErrorKind.Validation, null, fields);
  }

  public static Error Validation(string field, string message) =>
    Validation(new Dictionary<string, string[]>
    {
      [field] = [message]
    });

  public static Error Unauthorized(string? message = null) =>
    new(ErrorKind.Unauthorized, message);

  public static Error Forbidden(string? message = null) =>
    new(ErrorKind.Forbidden, message);

  public static Error NotFound(string? message = null) =>
    new(ErrorKind.NotFound, message);

  public static Error Conflict(string message) =>
    new(ErrorKind.Conflict, message);
}
