using CcsSso.Security.DbPersistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Security.DbPersistence
{
  public class DataContext : DbContext, IDataContext
    {
    public DataContext(DbContextOptions<DataContext> options)
            : base(options)
    {

    }

    public DbSet<RelyingParty> RelyingParties { get; set; }

    public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
      ValidateEntities();
      return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
      ValidateEntities();
      return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
      ValidateEntities();
      return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override int SaveChanges()
    {
      ValidateEntities();
      return base.SaveChanges();
    }

    private void ValidateEntities()
    {
      foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(e => new[] { EntityState.Added, EntityState.Modified }.Contains(e.State)))
      {
        if (entry.State == EntityState.Added)
        {
          entry.Entity.CreatedOnUtc = entry.Entity.LastUpdatedOnUtc = DateTime.UtcNow;
          entry.Entity.CreatedByUserId = entry.Entity.LastUpdatedByUserId = 0; // TODO after resolving context
        }
        else
        {
          entry.Entity.LastUpdatedOnUtc = DateTime.UtcNow;
          entry.Entity.LastUpdatedByUserId = 0; // TODO after resolving context
        }
      }
    }
  }
}
