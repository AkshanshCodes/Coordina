using System.Net.Mail;
using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Auth.Contracts;
using Coordina.Api.Modules.Auth.Domain;
using Coordina.Api.Shared.Validation;

namespace Coordina.Api.Modules.Auth.Application;

public sealed class AuthService(
  IUserStore users,
  IPasswordHasher passwordHasher,
  IAccessTokenIssuer accessTokenIssuer,
  TimeProvider clock) : IAuthService
{
  private const string DuplicateEmailMessage =
    "An account already exists with this email.";

  public async Task<Result<AuthResponse>> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken)
  {
    var errors = ValidateLogin(request.Email, request.Password)
      .Required(nameof(request.Name), request.Name, "Name", minLength: 2);

    if (errors.HasErrors)
    {
      return errors.ToError();
    }

    var normalizedEmail = NormalizeEmail(request.Email);
    var existingUser = await users.FindByEmailAsync(
      normalizedEmail,
      cancellationToken);

    if (existingUser is not null)
    {
      return Error.Conflict(DuplicateEmailMessage);
    }

    var user = new UserAccount(
      Guid.NewGuid(),
      request.Name.Trim(),
      request.Email.Trim(),
      normalizedEmail,
      passwordHasher.Hash(request.Password),
      clock.GetUtcNow());

    var created = await users.CreateAsync(user, cancellationToken);

    return created
      ? Result.Success(CreateAuthResponse(user))
      : Error.Conflict(DuplicateEmailMessage);
  }

  public async Task<Result<AuthResponse>> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken)
  {
    var errors = ValidateLogin(request.Email, request.Password);

    if (errors.HasErrors)
    {
      return errors.ToError();
    }

    var user = await users.FindByEmailAsync(
      NormalizeEmail(request.Email),
      cancellationToken);

    if (user is null
      || !passwordHasher.Verify(request.Password, user.PasswordHash))
    {
      return Error.Unauthorized();
    }

    return Result.Success(CreateAuthResponse(user));
  }

  private AuthResponse CreateAuthResponse(UserAccount user)
  {
    var token = accessTokenIssuer.Issue(user);

    return new AuthResponse(
      token.AccessToken,
      token.ExpiresAt,
      new CurrentUserResponse(user.Id.ToString(), user.Email, user.Name));
  }

  private static ValidationErrors ValidateLogin(string? email, string? password) =>
    new ValidationErrors()
      .AddIf(
        string.IsNullOrWhiteSpace(email) || !IsValidEmail(email),
        nameof(LoginRequest.Email),
        "A valid email is required.")
      .AddIf(
        string.IsNullOrWhiteSpace(password) || password.Length < 8,
        nameof(LoginRequest.Password),
        "Password must be at least 8 characters.");

  private static bool IsValidEmail(string email)
  {
    try
    {
      _ = new MailAddress(email);
      return true;
    }
    catch (FormatException)
    {
      return false;
    }
  }

  private static string NormalizeEmail(string email) =>
    email.Trim().ToUpperInvariant();
}
