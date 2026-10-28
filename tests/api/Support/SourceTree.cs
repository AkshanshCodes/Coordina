using System.Text.RegularExpressions;

namespace Coordina.Api.Tests.Support;

/// <summary>Scans API source files for architecture and convention tests.</summary>
internal static class SourceTree
{
  private static readonly Lazy<string> ApiRootPath = new(FindApiRoot);

  public static string ApiRoot => ApiRootPath.Value;

  /// <summary>
  /// Returns <c>path:line: code</c> for every line under <paramref name="folder"/>
  /// (relative to <c>src/api</c>) that matches <paramref name="pattern"/>.
  /// Generated EF migrations are skipped.
  /// </summary>
  public static string[] FindLines(
    string folder,
    Regex pattern,
    Func<string, bool>? includeFile = null)
  {
    var root = Path.Combine(ApiRoot, folder);

    Assert.True(Directory.Exists(root), $"Missing source folder '{root}'.");

    return Directory
      .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
      .Select(path => Path.GetRelativePath(ApiRoot, path).Replace('\\', '/'))
      .Where(path => !path.Contains("/Migrations/", StringComparison.Ordinal))
      .Where(path => includeFile?.Invoke(path) ?? true)
      .SelectMany(path => File.ReadLines(Path.Combine(ApiRoot, path))
        .Select((line, index) => (line, index))
        .Where(entry => pattern.IsMatch(entry.line))
        .Select(entry => $"{path}:{entry.index + 1}: {entry.line.Trim()}"))
      .ToArray();
  }

  public static string Describe(string rule, string[] violations) =>
    rule + Environment.NewLine + string.Join(Environment.NewLine, violations);

  private static string FindApiRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null
      && !File.Exists(Path.Combine(directory.FullName, "Coordina.sln")))
    {
      directory = directory.Parent;
    }

    Assert.NotNull(directory);
    return Path.Combine(directory.FullName, "src", "api");
  }
}
