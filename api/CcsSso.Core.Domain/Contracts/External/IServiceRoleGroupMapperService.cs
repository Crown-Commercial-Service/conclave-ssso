using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IServiceRoleGroupMapperService
  {
    Task<List<CcsServiceRoleGroup>> CssRolesToServiceRoleGroupsAsync(List<int> roleIds);

    Task<List<CcsServiceRoleGroup>> OrgRolesToServiceRoleGroupsAsync(List<int> roleIds);

    Task<List<CcsAccessRole>> ServiceRoleGroupsToCssRolesAsync(List<int> serviceRoleGroupIds);

    Task<List<OrganisationEligibleRole>> ServiceRoleGroupsToOrgRolesAsync(List<int> serviceRoleGroupIds, string organisationId);
  }
}
