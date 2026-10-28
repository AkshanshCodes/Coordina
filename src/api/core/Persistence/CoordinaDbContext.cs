using Microsoft.EntityFrameworkCore;

namespace Coordina.Api.Core.Persistence;

/// <summary>
/// Shared unit of work. Modules own their entities and register them through
/// <see cref="IEntityTypeConfiguration{TEntity}"/> classes; stores reach their
/// tables with <c>Set&lt;TEntity&gt;()</c>, so the core never references modules.
/// </summary>
public sealed class CoordinaDbContext(DbContextOptions<CoordinaDbContext> options)
  : DbContext(options)
{
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoordinaDbContext).Assembly);
  }
}
