using Coordina.Api.Core.Results;

namespace Coordina.Api.Core.Http;

/// <summary>
/// Single translation point from use-case results to HTTP responses, so every
/// module answers errors with the same status codes and body shapes.
/// </summary>
public static class ResultHttpExtensions
{
  public static IResult ToOkResult<T>(this Result<T> result)
  {
    ArgumentNullException.ThrowIfNull(result);

    return result.IsSuccess
      ? TypedResults.Ok(result.Value)
      : result.Error.ToHttpResult();
  }

  public static IResult ToCreatedResult<T>(
    this Result<T> result,
    Func<T, string> location)
  {
    ArgumentNullException.ThrowIfNull(result);
    ArgumentNullException.ThrowIfNull(location);

    return result.IsSuccess
      ? TypedResults.Created(location(result.Value), result.Value)
      : result.Error.ToHttpResult();
  }

  public static IResult ToNoContentResult(this Result result)
  {
    ArgumentNullException.ThrowIfNull(result);

    return result.IsSuccess
      ? TypedResults.NoContent()
      : result.Error.ToHttpResult();
  }

  public static IResult ToHttpResult(this Error error)
  {
    ArgumentNullException.ThrowIfNull(error);

    return error.Kind switch
    {
      ErrorKind.Validation => TypedResults.ValidationProblem(error.Fields),
      ErrorKind.Unauthorized => TypedResults.Unauthorized(),
      ErrorKind.Forbidden => TypedResults.Forbid(),
      ErrorKind.NotFound => error.Message is null
        ? TypedResults.NotFound()
        : TypedResults.NotFound(new ErrorResponse(error.Message)),
      ErrorKind.Conflict => TypedResults.Conflict(
        new ErrorResponse(error.Message ?? "The request conflicts with the current state.")),
      _ => throw new ArgumentOutOfRangeException(
        nameof(error),
        error.Kind,
        "Unknown error kind.")
    };
  }
}
