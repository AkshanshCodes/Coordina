using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Auth.Contracts;

namespace Coordina.Api.Modules.Auth.Application;

public interface IAuthService
{
  Task<Result<AuthResponse>> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken);

  Task<Result<AuthResponse>> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken);
}
