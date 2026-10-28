namespace Coordina.Api.Modules.Boards.Application.Graph;

public sealed class GraphEngineException : Exception
{
  public GraphEngineException()
  {
  }

  public GraphEngineException(string message)
    : base(message)
  {
  }

  public GraphEngineException(string message, Exception innerException)
    : base(message, innerException)
  {
  }

  public GraphEngineException(string operation, int status)
    : base($"Native graph operation '{operation}' failed with status {status}.")
  {
    Operation = operation;
    Status = status;
  }

  public string? Operation { get; }

  public int Status { get; }
}
