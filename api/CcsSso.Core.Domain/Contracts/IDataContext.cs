using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IDataContext
  {
    DbSet<AuditLog> AuditLog { get; set; }

    DbSet<Party> Party { get; set; }

    DbSet<PartyType> PartyType { get; set; }

    DbSet<Organisation> Organisation { get; set; }

    DbSet<TradingOrganisation> TradingOrganisation { get; set; }

    DbSet<EnterpriseType> EnterpriseType { get; set; }

    DbSet<OrganisationEnterpriseType> OrganisationEnterpriseType { get; set; }

    DbSet<ProcurementGroup> ProcurementGroup { get; set; }

    DbSet<Person> Person { get; set; }

    DbSet<User> User { get; set; }

    DbSet<OrganisationUserGroup> OrganisationUserGroup { get; set; }

    DbSet<UserGroupMembership> UserGroupMembership { get; set; }

    DbSet<UserSettingType> UserSettingType { get; set; }

    DbSet<UserSetting> UserSetting { get; set; }

    DbSet<IdentityProvider> IdentityProvider { get; set; }

    DbSet<CcsAccessRole> CcsAccessRole { get; set; }

    DbSet<UserAccessRole> UserAccessRole { get; set; }

    DbSet<OrganisationAccessRole> OrganisationAccessRole { get; set; }

    // TODO - clarify
    // DbSet<UserGroupAccessRole> UserGroupAccessRole { get; set; }

    DbSet<ContactPoint> ContactPoint { get; set; }

    DbSet<ContactDetail> ContactDetail { get; set; }

    DbSet<PhysicalAddress> PhysicalAddress { get; set; }

    DbSet<VirtualAddress> VirtualAddress { get; set; }

    DbSet<VirtualAddressType> VirtualAddressType { get; set; }

    DbSet<ContactPointReason> ContactPointReason { get; set; }

    DbSet<OrganisationGroupEligibleRole> OrganisationGroupEligibleRole { get; set; }

    DbSet<CcsService> CcsService { get; set; }

    DbSet<CcsServiceLogin> CcsServiceLogin { get; set; }

    DbSet<ServicePermission> ServicePermission { get; set; }

    DbSet<ServiceRolePermission> ServiceRolePermission { get; set; }

    DbSet<IdamUserLoginRole> IdamUserLoginRole { get; set; }

    DbSet<IdamUserLogin> IdamUserLogin { get; set; }

    DbSet<SiteContact> SiteContact { get; set; }

    DbSet<OrganisationEligibleRole> OrganisationEligibleRole { get; set; }

    DbSet<OrganisationEligibleIdentityProvider> OrganisationEligibleIdentityProvider { get; set; }

    Task<PagedResultSet<T>> GetPagedResultAsync<T>(IQueryable<T> query, ResultSetCriteria resultSetCriteria);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  }
}
