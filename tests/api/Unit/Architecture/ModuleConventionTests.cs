using System.Text.RegularExpressions;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests.Unit.Architecture;

/// <summary>
/// Conventions every module follows so reviews can focus on behavior. Each
/// rule points to the core or shared building block to use instead.
/// </summary>
public sealed partial class ModuleConventionTests
{
  [Fact]
  public void Modules_DoNotReadTheSystemClock()
  {
    AssertNone(
      SourceTree.FindLines("modules", SystemClock()),
      "Inject TimeProvider (core) instead of reading the system clock:");
  }

  [Fact]
  public void Modules_DoNotParseClaims_OutsideAuth()
  {
    AssertNone(
      SourceTree.FindLines(
        "modules",
        ClaimParsing(),
        path => !path.StartsWith("modules/auth/", StringComparison.Ordinal)),
      "Bind CurrentUser (core) instead of reading claims:");
  }

  [Fact]
  public void Modules_UseTheDbContext_OnlyInInfrastructure()
  {
    AssertNone(
      SourceTree.FindLines(
        "modules",
        DbContextUsage(),
        path => !path.Contains("/Infrastructure/", StringComparison.Ordinal)),
      "Only Infrastructure stores may touch CoordinaDbContext:");
  }

  [Fact]
  public void Modules_DoNotDeclareTheirOwnResultTypes()
  {
    AssertNone(
      SourceTree.FindLines("modules", ResultTypeDeclaration()),
      "Return Result/Result<T> and Error (core) instead of module result types:");
  }

  private static void AssertNone(string[] violations, string rule) =>
    Assert.True(violations.Length == 0, SourceTree.Describe(rule, violations));

  [GeneratedRegex(@"\b(DateTime|DateTimeOffset)\.(UtcNow|Now|Today)\b")]
  private static partial Regex SystemClock();

  [GeneratedRegex(@"\bClaimTypes\.|\bFindFirstValue\(")]
  private static partial Regex ClaimParsing();

  [GeneratedRegex(@"\bCoordinaDbContext\b")]
  private static partial Regex DbContextUsage();

  [GeneratedRegex(@"\b(class|record|struct|enum|interface)\s+\w*Result(Status)?\b")]
  private static partial Regex ResultTypeDeclaration();
}
