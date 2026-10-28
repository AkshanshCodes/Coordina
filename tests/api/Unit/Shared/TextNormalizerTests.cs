using Coordina.Api.Shared.Text;

namespace Coordina.Api.Tests.Unit.Shared;

public sealed class TextNormalizerTests
{
  [Theory]
  [InlineData(null, null)]
  [InlineData("", null)]
  [InlineData("   ", null)]
  [InlineData("  value ", "value")]
  public void TrimToNull_NormalizesBlankInput(string? value, string? expected)
  {
    Assert.Equal(expected, TextNormalizer.TrimToNull(value));
  }

  [Fact]
  public void DistinctNonBlank_TrimsDeduplicatesAndLimits()
  {
    var values = TextNormalizer.DistinctNonBlank(
      [" Backend ", "backend", "", null, "Docs", "Design"],
      2);

    Assert.Equal(["Backend", "Docs"], values);
  }

  [Fact]
  public void DistinctNonBlank_WithNullValues_ReturnsEmpty()
  {
    Assert.Empty(TextNormalizer.DistinctNonBlank(null, 5));
  }
}
