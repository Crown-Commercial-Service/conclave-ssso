using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.DbDomain.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.DbPersistence
{
  public class DataContext : DbContext, IDataContext
  {
    public DataContext(DbContextOptions<DataContext> options)
            : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder
        .Entity<AdapterConsumer>()
        .HasIndex(a => a.ClientId)
        .IsUnique();

      modelBuilder
        .Entity<AdapterConsumerEntity>()
        .HasIndex(ce => new { ce.Name, ce.AdapterConsumerId })
        .IsUnique();
    }

    public DbSet<AdapterConclaveAttributeMapping> AdapterConclaveAttributeMappings { get; set; }

    public DbSet<AdapterConsumer> AdapterConsumers { get; set; }

    public DbSet<AdapterConsumerEntity> AdapterConsumerEntities { get; set; }

    public DbSet<AdapterConsumerEntityAttribute> AdapterConsumerEntityAttributes { get; set; }

    public DbSet<AdapterFormat> AdapterFormats { get; set; }

    public DbSet<AdapterSubscription> AdapterSubscriptions { get; set; }

    public DbSet<ConclaveEntity> ConclaveEntities { get; set; }

    public DbSet<ConclaveEntityAttribute> ConclaveEntityAttributes { get; set; }

    public DbSet<AdapterConsumerSubscriptionAuthMethod> AdapterConsumerSubscriptionAuthMethods { get; set; }

    public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
      return await base.SaveChangesAsync(cancellationToken);
    }
  }
}
