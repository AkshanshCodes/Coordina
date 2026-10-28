using Coordina.Api.Core.Results;

namespace Coordina.Api.Tests.Unit.Core;

public sealed class ResultTests
{
  [Fact]
  public void Success_ExposesValue()
  {
    var result = Result.Success(42);

    Assert.True(result.IsSuccess);
    Assert.Null(result.Error);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void ImplicitError_ProducesFailedResult()
  {
    Result<int> result = Error.NotFound("Missing.");

    Assert.False(result.IsSuccess);
    Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    Assert.Equal("Missing.", result.Error.Message);
  }

  [Fact]
  public void Value_OnFailedResult_Throws()
  {
    Result<int> result = Error.Conflict("Busy.");

    Assert.Throws<InvalidOperationException>(() => result.Value);
  }

  [Fact]
  public void FailedGenericResult_CanFlowAsResult()
  {
    Result<string> typed = Error.Forbidden();
    Result untyped = typed;

    Assert.False(untyped.IsSuccess);
    Assert.Equal(ErrorKind.Forbidden, untyped.Error.Kind);
  }

  [Fact]
  public void ValidationError_ForSingleField_KeepsFieldMessage()
  {
    var error = Error.Validation("Name", "Name is required.");

    Assert.Equal(ErrorKind.Validation, error.Kind);
    Assert.Equal(["Name is required."], error.Fields["Name"]);
  }

  [Fact]
  public void NonValidationError_HasNoFields()
  {
    Assert.Empty(Error.NotFound().Fields);
  }
}
