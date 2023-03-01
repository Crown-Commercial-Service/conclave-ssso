using CcsSso.Domain.Contracts;
using CcsSso.DbModel.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Contracts;

namespace CcsSso.DbPersistence
{
  public class DataContext : DbContext, IDataContext
  {
    private readonly RequestContext _requestContext;
    private readonly IDateTimeService _dateTimeService;
    public DataContext(DbContextOptions<DataContext> options, RequestContext requestContext, IDateTimeService dateTimeService)
            : base(options)
    {
      _requestContext = requestContext;
      _dateTimeService = dateTimeService;
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
      modelBuilder.Entity<User>()
       .HasIndex(u => u.UserName);
      modelBuilder.Entity<BulkUploadDetail>()
       .HasIndex(o => o.FileKeyId)
       .IsUnique();
    }

    public async Task<PagedResultSet<T>> GetPagedResultAsync<T>(IQueryable<T> query, ResultSetCriteria resultSetCriteria)
    {
      var currentPage = resultSetCriteria.CurrentPage;
      var pageSize = resultSetCriteria.PageSize;

      var result = new PagedResultSet<T>
      {
        CurrentPage = currentPage,
        RowCount = await query.CountAsync()
      };
      var pageCount = (double)result.RowCount / pageSize;
      result.PageCount = (int)Math.Ceiling(pageCount);
      var skip = (currentPage - 1) * pageSize;
      result.Results = await query.Skip(skip).Take(pageSize).ToListAsync();

      return result;
    }

    public DbSet<AuditLog> AuditLog { get; set; }

    public DbSet<Party> Party { get; set; }

    public DbSet<PartyType> PartyType { get; set; }

    public DbSet<Organisation> Organisation { get; set; }

    public DbSet<TradingOrganisation> TradingOrganisation { get; set; }

    public DbSet<EnterpriseType> EnterpriseType { get; set; }

    public DbSet<OrganisationEnterpriseType> OrganisationEnterpriseType { get; set; }

    public DbSet<ProcurementGroup> ProcurementGroup { get; set; }

    public DbSet<Person> Person { get; set; }

    public DbSet<User> User { get; set; }

    public DbSet<OrganisationUserGroup> OrganisationUserGroup { get; set; }

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

    public DbSet<OrganisationGroupEligibleRole> OrganisationGroupEligibleRole { get; set; }

    public DbSet<CcsService> CcsService { get; set; }

    public DbSet<CcsServiceLogin> CcsServiceLogin { get; set; }

    public DbSet<ServicePermission> ServicePermission { get; set; }

    public DbSet<ServiceRolePermission> ServiceRolePermission { get; set; }

    public DbSet<IdamUserLoginRole> IdamUserLoginRole { get; set; }

    public DbSet<IdamUserLogin> IdamUserLogin { get; set; }

    public DbSet<SiteContact> SiteContact { get; set; }

    public DbSet<ExternalServiceRoleMapping> ExternalServiceRoleMapping { get; set; }

    public DbSet<OrganisationEligibleRole> OrganisationEligibleRole { get; set; }

    public DbSet<OrganisationEligibleIdentityProvider> OrganisationEligibleIdentityProvider { get; set; }

    public DbSet<UserIdentityProvider> UserIdentityProvider { get; set; }

    public DbSet<BulkUploadDetail> BulkUploadDetail { get; set; }

    public DbSet<CountryDetails> CountryDetails { get; set; }

    // #Auto validation
    public DbSet<OrganisationAudit> OrganisationAudit { get; set; }

    public DbSet<OrganisationAuditEvent> OrganisationAuditEvent { get; set; }

    public DbSet<AutoValidationRole> AutoValidationRole { get; set; }

    public DbSet<OrganisationEligibleRolePending> OrganisationEligibleRolePending { get; set; }

    public DbSet<RoleApprovalConfiguration> RoleApprovalConfiguration { get; set; }

    public DbSet<UserAccessRolePending> UserAccessRolePending { get; set; }
    public DbSet<CcsServiceRoleGroup> CcsServiceRoleGroup { get; set; }

    public DbSet<CcsServiceRoleMapping> CcsServiceRoleMapping { get; set; }

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
          entry.Entity.CreatedOnUtc = entry.Entity.LastUpdatedOnUtc = _dateTimeService.GetUTCNow();
          entry.Entity.CreatedUserId = entry.Entity.LastUpdatedUserId = _requestContext.UserId;
        }
        else
        {
          entry.Entity.LastUpdatedOnUtc = _dateTimeService.GetUTCNow();
          entry.Entity.LastUpdatedUserId = _requestContext.UserId;
        }
      }
    }
  }
}
