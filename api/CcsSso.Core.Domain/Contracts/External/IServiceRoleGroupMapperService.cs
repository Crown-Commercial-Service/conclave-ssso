using CcsSso.Core.DbModel.Entity;
using CcsSso.DbModel.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IServiceRoleGroupMapperService
  {
    Task<List<CcsServiceRoleGroup>> CcsRolesToServiceRoleGroupsAsync(List<int> roleIds);
    
    Task<List<CcsServiceRoleGroup>> OrgRolesToServiceRoleGroupsAsync(List<int> roleIds);

    Task<List<CcsAccessRole>> ServiceRoleGroupsToCcsRolesAsync(List<int> serviceRoleGroupIds);

    Task<List<OrganisationEligibleRole>> ServiceRoleGroupsToOrgRolesAsync(List<int> serviceRoleGroupIds, string organisationId);

    Task<List<CcsServiceRoleGroup>> ServiceRoleGroupsWithApprovalRequiredRoleAsync();

    Task RemoveApprovalRequiredRoleGroupOtherRolesAsync(List<OrganisationEligibleRole> organisationEligibleRoles);
  }
}
