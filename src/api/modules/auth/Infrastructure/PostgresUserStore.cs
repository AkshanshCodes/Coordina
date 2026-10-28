using Coordina.Api.Core.Persistence;
using Coordina.Api.Modules.Auth.Application;
using Coordina.Api.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Coordina.Api.Modules.Auth.Infrastructure;

public sealed class PostgresUserStore(CoordinaDbContext dbContext) : IUserStore
{
  private DbSet<AuthUserEntity> AuthUsers => dbContext.Set<AuthUserEntity>();

  public async Task<UserAccount?> FindByEmailAsync(
    string normalizedEmail,
    CancellationToken cancellationToken)
  {
    var user = await AuthUsers
      .AsNoTracking()
      .SingleOrDefaultAsync(
        user => user.NormalizedEmail == normalizedEmail,
        cancellationToken);

    if (user is null)
    {
      return null;
    }

    return ToDomain(user);
  }

  public async Task<bool> CreateAsync(
    UserAccount user,
    CancellationToken cancellationToken)
  {
    AuthUsers.Add(new AuthUserEntity
    {
      Id = user.Id,
      Name = user.Name,
      Email = user.Email,
      NormalizedEmail = user.NormalizedEmail,
      PasswordHash = user.PasswordHash,
      CreatedAt = user.CreatedAt
    });

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException exception)
      when (exception.InnerException is PostgresException
      {
        SqlState: PostgresErrorCodes.UniqueViolation
      })
    {
      return false;
    }

    return true;
  }

  private static UserAccount ToDomain(AuthUserEntity user)
  {
    return new UserAccount(
      user.Id,
      user.Name,
      user.Email,
      user.NormalizedEmail,
      user.PasswordHash,
      user.CreatedAt);
  }
}
