namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record CreateBoardListRequest(string? Title, string? TaskStatus = null);
