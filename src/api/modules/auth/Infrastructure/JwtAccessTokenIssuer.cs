using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Coordina.Api.Modules.Auth.Application;
using Coordina.Api.Modules.Auth.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Coordina.Api.Modules.Auth.Infrastructure;

public sealed class JwtAccessTokenIssuer(
  AuthOptions options,
  TimeProvider clock) : IAccessTokenIssuer
{
  public IssuedAccessToken Issue(UserAccount user)
  {
    var expiresAt = clock.GetUtcNow().AddMinutes(
      options.AccessTokenMinutes);
    var signingCredentials = new SigningCredentials(
      new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
      SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: options.Issuer,
      audience: options.Audience,
      claims:
      [
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name)
      ],
      expires: expiresAt.UtcDateTime,
      signingCredentials: signingCredentials);

    return new IssuedAccessToken(
      new JwtSecurityTokenHandler().WriteToken(token),
      expiresAt);
  }
}
