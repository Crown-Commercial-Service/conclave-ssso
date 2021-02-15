using CcsSso.Domain.Contracts;
using CcsSso.DbModel.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CcsSso.DbPersistence
{
  public class DataContext : DbContext, IDataContext
  {
    public DataContext(DbContextOptions<DataContext> options)
            : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      //var converter = new ValueConverter<System.Numerics.BigInteger, long>(
      //  x => (long)x,
      //  x => new System.Numerics.BigInteger(x));
      //modelBuilder
      //    .Entity<IdentityProvider>()
      //    .Property(e => e.Id)
      //    .HasConversion(converter);
      //modelBuilder
      //    .Entity<User>()
      //    .Property(e => e.IdentityProviderId)
      //    .HasConversion(converter);
      modelBuilder
          .Entity<Organisation>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<Organisation>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder
          .Entity<IdentityProvider>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<IdentityProvider>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder
          .Entity<User>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<User>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder
          .Entity<Party>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<Party>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder
          .Entity<Person>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<Person>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder
          .Entity<ContactDetail>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<ContactDetail>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder
          .Entity<ContactPoint>()
          .HasKey(x => x.Id);
      modelBuilder
          .Entity<ContactPoint>()
          .Property(x => x.Id)
          .ValueGeneratedOnAdd();
      modelBuilder.Entity<Organisation>()
        .HasIndex(o => o.CiiOrganisationId);
    }

    public DbSet<Party> Party { get; set; }

    public DbSet<PartyType> PartyType { get; set; }

    public DbSet<Organisation> Organisation { get; set; }

    public DbSet<TradingOrganisation> TradingOrganisation { get; set; }

    public DbSet<EnterpriseType> EnterpriseType { get; set; }

    public DbSet<OrganisationEnterpriseType> OrganisationEnterpriseType { get; set; }

    public DbSet<ProcurementGroup> ProcurementGroup { get; set; }

    public DbSet<Person> Person { get; set; }

    public DbSet<User> User { get; set; }

    public DbSet<UserGroup> UserGroup { get; set; }

    public DbSet<UserGroupMembership> UserGroupMembership { get; set; }

    public DbSet<UserSettingType> UserSettingType { get; set; }

    public DbSet<UserSetting> UserSetting { get; set; }

    public DbSet<IdentityProvider> IdentityProvider { get; set; }

    public DbSet<CcsAccessRole> CcsAccessRole { get; set; }

    public DbSet<UserAccessRole> UserAccessRole { get; set; }

    public DbSet<OrganisationAccessRole> OrganisationAccessRole { get; set; }

    // TODO - clarify
    // public DbSet<UserGroupAccessRole> UserGroupAccessRole { get; set; }

    public DbSet<ContactPoint> ContactPoint { get; set; }

    public DbSet<ContactDetail> ContactDetail { get; set; }

    public DbSet<PhysicalAddress> PhysicalAddress { get; set; }

    public DbSet<VirtualAddress> VirtualAddress { get; set; }

    public DbSet<VirtualAddressType> VirtualAddressType { get; set; }

    public DbSet<ContactPointReason> ContactPointReason { get; set; }

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
          entry.Entity.CreatedPartyId = entry.Entity.LastUpdatedPartyId = 0; // TODO after resolving context
        }
        else
        {
          entry.Entity.LastUpdatedOnUtc = DateTime.UtcNow;
          entry.Entity.LastUpdatedPartyId = 0; // TODO after resolving context
        }
      }
    }
  }
}
