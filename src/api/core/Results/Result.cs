using System.Diagnostics.CodeAnalysis;

namespace Coordina.Api.Core.Results;

/// <summary>
/// Outcome of a use case without a payload. An error converts implicitly,
/// so services can simply <c>return Error.NotFound();</c>.
/// </summary>
public class Result
{
  private static readonly Result SuccessResult = new(null);

  protected Result(Error? error)
  {
    Error = error;
  }

  public Error? Error { get; }

  [MemberNotNullWhen(false, nameof(Error))]
  public bool IsSuccess => Error is null;

  public static Result Success() => SuccessResult;

  public static Result<T> Success<T>(T value) => new(value);

  public static Result FromError(Error error)
  {
    ArgumentNullException.ThrowIfNull(error);

    return new Result(error);
  }

  public static implicit operator Result(Error error) => FromError(error);
}

/// <summary>Outcome of a use case that produces a value on success.</summary>
public sealed class Result<T> : Result
{
  private readonly T? value;

  internal Result(T value)
    : base(null)
  {
    this.value = value;
  }

  private Result(Error error)
    : base(error)
  {
  }

  public T Value => IsSuccess
    ? value!
    : throw new InvalidOperationException(
      "A failed result has no value. Check IsSuccess first.");

#pragma warning disable CA1000 // Factory mirrors the implicit conversion below.
  public static new Result<T> FromError(Error error)
#pragma warning restore CA1000
  {
    ArgumentNullException.ThrowIfNull(error);

    return new Result<T>(error);
  }

  public static implicit operator Result<T>(Error error) => FromError(error);
}
