namespace Coordina.Api.Core.Configuration;

public static class EnvFile
{
  public static void LoadNearest(string fileName = ".env")
  {
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
      var filePath = Path.Combine(directory.FullName, fileName);

      if (File.Exists(filePath))
      {
        Load(filePath);
        return;
      }

      directory = directory.Parent;
    }
  }

  private static void Load(string filePath)
  {
    foreach (var line in File.ReadLines(filePath))
    {
      var trimmedLine = line.Trim();

      if (string.IsNullOrWhiteSpace(trimmedLine)
        || trimmedLine.StartsWith('#'))
      {
        continue;
      }

      var separatorIndex = trimmedLine.IndexOf('=');

      if (separatorIndex <= 0)
      {
        continue;
      }

      var key = trimmedLine[..separatorIndex].Trim();
      var value = trimmedLine[(separatorIndex + 1)..].Trim().Trim('"');

      Environment.SetEnvironmentVariable(key, value);
    }
  }
}
