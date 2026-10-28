using Coordina.Api.Core.Http;
using Coordina.Api.Core.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Coordina.Api.Tests.Unit.Core;

public sealed class ResultHttpExtensionsTests
{
  [Fact]
  public void ToOkResult_OnSuccess_ReturnsOkWithValue()
  {
    var response = Result.Success("value").ToOkResult();

    var ok = Assert.IsType<Ok<string>>(response);
    Assert.Equal("value", ok.Value);
  }

  [Fact]
  public void ToCreatedResult_OnSuccess_UsesLocationFromValue()
  {
    var response = Result.Success(7).ToCreatedResult(id => $"/items/{id}");

    var created = Assert.IsType<Created<int>>(response);
    Assert.Equal("/items/7", created.Location);
    Assert.Equal(7, created.Value);
  }

  [Fact]
  public void ToNoContentResult_OnSuccess_ReturnsNoContent()
  {
    Assert.IsType<NoContent>(Result.Success().ToNoContentResult());
  }

  [Fact]
  public void ValidationError_ReturnsValidationProblemWithFields()
  {
    Result<string> result = Error.Validation("Title", "Title is required.");

    var problem = Assert.IsType<ValidationProblem>(result.ToOkResult());
    Assert.Equal(["Title is required."], problem.ProblemDetails.Errors["Title"]);
  }

  [Fact]
  public void NotFoundWithoutMessage_ReturnsEmptyNotFound()
  {
    Assert.IsType<NotFound>(Error.NotFound().ToHttpResult());
  }

  [Fact]
  public void NotFoundWithMessage_ReturnsMessageBody()
  {
    var notFound = Assert.IsType<NotFound<ErrorResponse>>(
      Error.NotFound("Gone.").ToHttpResult());

    Assert.Equal("Gone.", notFound.Value?.Message);
  }

  [Fact]
  public void Conflict_ReturnsMessageBody()
  {
    var conflict = Assert.IsType<Conflict<ErrorResponse>>(
      Error.Conflict("Busy.").ToHttpResult());

    Assert.Equal("Busy.", conflict.Value?.Message);
  }

  [Fact]
  public void Forbidden_ReturnsForbid()
  {
    Assert.IsType<ForbidHttpResult>(Error.Forbidden("No.").ToHttpResult());
  }

  [Fact]
  public void Unauthorized_ReturnsUnauthorized()
  {
    Assert.IsType<UnauthorizedHttpResult>(Error.Unauthorized().ToHttpResult());
  }

  [Fact]
  public void FailedResult_IsNotMappedAsSuccess()
  {
    Result result = Error.NotFound();

    Assert.IsType<NotFound>(result.ToNoContentResult());
  }
}
