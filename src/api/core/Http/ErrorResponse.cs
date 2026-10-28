namespace Coordina.Api.Core.Http;

/// <summary>Body returned for non-validation errors that carry a message.</summary>
public sealed record ErrorResponse(string Message);
