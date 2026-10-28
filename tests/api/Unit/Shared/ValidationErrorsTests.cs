using Coordina.Api.Core.Results;
using Coordina.Api.Shared.Validation;

namespace Coordina.Api.Tests.Unit.Shared;

public sealed class ValidationErrorsTests
{
  [Theory]
  [InlineData(null, "Name is required.")]
  [InlineData("   ", "Name is required.")]
  [InlineData(" a ", "Name must be at least 2 characters.")]
  [InlineData("abcdef", "Name must be 5 characters or fewer.")]
  public void Required_ReportsOneMessagePerRule(string? value, string expected)
  {
    var errors = new ValidationErrors()
      .Required("Name", value, "Name", minLength: 2, maxLength: 5);

    Assert.Equal([expected], errors.ToDictionary()["Name"]);
  }

  [Fact]
  public void Required_WithValidTrimmedValue_HasNoErrors()
  {
    var errors = new ValidationErrors()
      .Required("Name", "  abc  ", "Name", minLength: 2, maxLength: 3);

    Assert.False(errors.HasErrors);
  }

  [Fact]
  public void MaxLength_IgnoresMissingValue()
  {
    var errors = new ValidationErrors()
      .MaxLength("Description", null, "Description", 3);

    Assert.False(errors.HasErrors);
  }

  [Fact]
  public void MaxLength_WithLongValue_ReportsError()
  {
    var errors = new ValidationErrors()
      .MaxLength("Description", "abcd", "Description", 3);

    Assert.Equal(
      ["Description must be 3 characters or fewer."],
      errors.ToDictionary()["Description"]);
  }

  [Fact]
  public void AddIf_OnlyAddsWhenConditionHolds()
  {
    var errors = new ValidationErrors()
      .AddIf(false, "A", "Skipped.")
      .AddIf(true, "B", "Kept.");

    Assert.False(errors.Contains("A"));
    Assert.True(errors.Contains("B"));
  }

  [Fact]
  public void ToError_ProducesValidationError()
  {
    var error = new ValidationErrors()
      .Add("Title", "Title is required.")
      .ToError();

    Assert.Equal(ErrorKind.Validation, error.Kind);
    Assert.Equal(["Title is required."], error.Fields["Title"]);
  }

  public enum Priority
  {
    Low,
    High
  }

  [Fact]
  public void Code_WithBlankValue_IsAcceptedAsMissing()
  {
    var errors = new ValidationErrors()
      .Code<Priority>("Priority", " ", "Priority", out var parsed);

    Assert.False(errors.HasErrors);
    Assert.Null(parsed);
  }

  [Fact]
  public void Code_WithKnownValue_ReturnsParsedEnum()
  {
    new ValidationErrors().Code<Priority>("Priority", "high", "Priority", out var parsed);

    Assert.Equal(Priority.High, parsed);
  }

  [Fact]
  public void Code_WithUnknownValue_ListsAcceptedCodes()
  {
    var errors = new ValidationErrors()
      .Code<Priority>("Priority", "urgent", "Priority", out _);

    Assert.Equal(
      ["Priority must be LOW or HIGH."],
      errors.ToDictionary()["Priority"]);
  }

  [Fact]
  public void RequiredCode_WithBlankValue_ReportsRequired()
  {
    var errors = new ValidationErrors()
      .RequiredCode<Priority>("Priority", null, "Priority", out _);

    Assert.Equal(["Priority is required."], errors.ToDictionary()["Priority"]);
  }
}
