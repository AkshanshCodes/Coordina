using Coordina.Api.Shared.Enums;

namespace Coordina.Api.Tests.Unit.Shared;

public sealed class EnumCodeTests
{
  public enum SampleStatus
  {
    Todo,
    InProgress,
    AgileScrum
  }

  [Theory]
  [InlineData(SampleStatus.Todo, "TODO")]
  [InlineData(SampleStatus.InProgress, "IN_PROGRESS")]
  [InlineData(SampleStatus.AgileScrum, "AGILE_SCRUM")]
  public void Format_UsesUpperSnakeCase(SampleStatus value, string expected)
  {
    Assert.Equal(expected, EnumCode.Format(value));
  }

  [Theory]
  [InlineData("IN_PROGRESS")]
  [InlineData("in_progress")]
  [InlineData(" InProgress ")]
  [InlineData("INPROGRESS")]
  public void TryParse_AcceptsCodeVariants(string code)
  {
    Assert.True(EnumCode.TryParse<SampleStatus>(code, out var value));
    Assert.Equal(SampleStatus.InProgress, value);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("1")]
  [InlineData("DONE")]
  public void TryParse_RejectsUnknownOrNumericCodes(string? code)
  {
    Assert.False(EnumCode.TryParse<SampleStatus>(code, out _));
  }

  [Fact]
  public void Format_WithUndefinedValue_Throws()
  {
    Assert.Throws<ArgumentOutOfRangeException>(
      () => EnumCode.Format((SampleStatus)42));
  }

  [Fact]
  public void Describe_ListsCodesInDeclarationOrder()
  {
    Assert.Equal("TODO, IN_PROGRESS, or AGILE_SCRUM", EnumCode.Describe<SampleStatus>());
  }

  [Fact]
  public void Parse_WithUnknownCode_Throws()
  {
    Assert.Throws<FormatException>(() => EnumCode.Parse<SampleStatus>("NOPE"));
  }
}
