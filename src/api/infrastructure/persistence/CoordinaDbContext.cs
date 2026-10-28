using Coordina.Api.Modules.Auth.Infrastructure;
using Coordina.Api.Modules.Projects.Infrastructure;
using Coordina.Api.Modules.Boards.Infrastructure;
using Coordina.Api.Modules.Workspaces.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Coordina.Api.Infrastructure.Persistence;

public sealed class CoordinaDbContext(DbContextOptions<CoordinaDbContext> options)
  : DbContext(options)
{
  public DbSet<AuthUserEntity> AuthUsers => Set<AuthUserEntity>();
  public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();
  public DbSet<WorkspaceMemberEntity> WorkspaceMembers => Set<WorkspaceMemberEntity>();
  public DbSet<WorkspaceInviteEntity> WorkspaceInvites => Set<WorkspaceInviteEntity>();
  public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
  public DbSet<BoardEntity> Boards => Set<BoardEntity>();
  public DbSet<BoardListEntity> BoardLists => Set<BoardListEntity>();
  public DbSet<BoardCardEntity> BoardCards => Set<BoardCardEntity>();
  public DbSet<BoardCardAssigneeEntity> BoardCardAssignees =>
    Set<BoardCardAssigneeEntity>();
  public DbSet<BoardCardCommentEntity> BoardCardComments =>
    Set<BoardCardCommentEntity>();
  public DbSet<BoardCardSubtaskEntity> BoardCardSubtasks =>
    Set<BoardCardSubtaskEntity>();
  public DbSet<BoardCardDependencyEntity> BoardCardDependencies =>
    Set<BoardCardDependencyEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoordinaDbContext).Assembly);
  }
}
