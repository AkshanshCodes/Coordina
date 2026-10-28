using System.Text.RegularExpressions;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests.Unit.Architecture;

/// <summary>
/// Guards the API layering: modules → shared → core. Core is the kernel every
/// module plugs into, so it must never reach into shared helpers or modules.
/// EF migrations are generated from the composed model and are exempt.
/// </summary>
public sealed partial class LayerDependencyTests
{
  [Fact]
  public void Core_DoesNotDependOnSharedOrModules()
  {
    var violations = SourceTree.FindLines("core", CoreForbiddenReference());

    Assert.True(
      violations.Length == 0,
      SourceTree.Describe("core must not reference shared or modules:", violations));
  }

  [Fact]
  public void Shared_DoesNotDependOnModules()
  {
    var violations = SourceTree.FindLines("shared", ModuleReference());

    Assert.True(
      violations.Length == 0,
      SourceTree.Describe("shared must not reference modules:", violations));
  }

  [GeneratedRegex(@"\bCoordina\.Api\.(Shared|Modules)\b")]
  private static partial Regex CoreForbiddenReference();

  [GeneratedRegex(@"\bCoordina\.Api\.Modules\b")]
  private static partial Regex ModuleReference();
}
