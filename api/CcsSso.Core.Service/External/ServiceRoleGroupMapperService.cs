using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class ServiceRoleGroupMapperService : IServiceRoleGroupMapperService
  {
    private readonly IDataContext _dataContext;

    public ServiceRoleGroupMapperService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task<List<CcsServiceRoleGroup>> CcsRolesToServiceRoleGroupsAsync(List<int> roleIds)
    {
      if (!roleIds.Any())
      {
        return new List<CcsServiceRoleGroup>();
      }

      List<CcsServiceRoleGroup> selectedServiceRoleGroups = new();

      var serviceRoleGroups = await _dataContext.CcsServiceRoleGroup
        .Include(g => g.CcsServiceRoleMappings)
        .Where(x => !x.IsDeleted)
        .ToListAsync();

      foreach (var serviceRoleGroup in serviceRoleGroups)
      {
        if (serviceRoleGroup.CcsServiceRoleMappings.Any(x => roleIds.Contains(x.CcsAccessRoleId)))
        {
          selectedServiceRoleGroups.Add(serviceRoleGroup);
        }
      }

      selectedServiceRoleGroups = selectedServiceRoleGroups.OrderBy(x => x.DisplayOrder).ToList();

      return selectedServiceRoleGroups;
    }

    public async Task<List<CcsServiceRoleGroup>> OrgRolesToServiceRoleGroupsAsync(List<int> roleIds) 
    {
      if (!roleIds.Any())
      {
        return new List<CcsServiceRoleGroup>();
      }

      roleIds = await OrgRolesToCcsRoles(roleIds);

      return await CcsRolesToServiceRoleGroupsAsync(roleIds);
    }

    public async Task<List<CcsAccessRole>> ServiceRoleGroupsToCcsRolesAsync(List<int> serviceRoleGroupIds)
    {
      List<CcsAccessRole> ccsAccessRole = new List<CcsAccessRole>();

      if (!serviceRoleGroupIds.Any())
      {
        return ccsAccessRole;
      }

      var serviceRoleGroups = await _dataContext.CcsServiceRoleGroup
        .Include(g => g.CcsServiceRoleMappings).ThenInclude(g => g.CcsAccessRole)
        .Where(x => !x.IsDeleted).ToListAsync();

      foreach (var serviceRoleGroupId in serviceRoleGroupIds)
      {
        var ccsAccessRoles = serviceRoleGroups.FirstOrDefault(x => x.Id == serviceRoleGroupId)?.CcsServiceRoleMappings
                             .Select(x => x.CcsAccessRole)
                             .ToList();

        if (ccsAccessRoles != null)
        {
          ccsAccessRole.AddRange(ccsAccessRoles);
        }
        else
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidService);
        }
      }

      return ccsAccessRole
        .Distinct()
        .ToList();
    }

    public async Task<List<OrganisationEligibleRole>> ServiceRoleGroupsToOrgRolesAsync(List<int> serviceRoleGroupIds, string organisationId)
    {
      if (!serviceRoleGroupIds.Any())
      {
        return new List<OrganisationEligibleRole>();
      }

      var ccsAccessRoles = await ServiceRoleGroupsToCcsRolesAsync(serviceRoleGroupIds);

      var ccsAccessRoleIds = ccsAccessRoles.Select(x => x.Id).ToList();

      var organisationEligibleRoles = await _dataContext.OrganisationEligibleRole
        .Include(g => g.Organisation)
        .Where(x => !x.IsDeleted && ccsAccessRoleIds.Contains(x.CcsAccessRoleId) && x.Organisation.CiiOrganisationId == organisationId)
        .ToListAsync();

      return organisationEligibleRoles;
    }

    public async Task<List<CcsServiceRoleGroup>> ServiceRoleGroupsWithApprovalRequiredRoleAsync() 
    {
      var serviceRoleGroups = await _dataContext.CcsServiceRoleGroup
        .Include(g => g.CcsServiceRoleMappings).ThenInclude(g => g.CcsAccessRole)
        .Where(x => !x.IsDeleted && x.CcsServiceRoleMappings.Any(y => y.CcsAccessRole.ApprovalRequired == 1)).ToListAsync();

      return serviceRoleGroups;
    }

    // This method will remove roles that are part of approval required Service Role Group but it self not required approval
    // This normal roles will be assigned together with approval required role, once it is approved.
    public async Task RemoveApprovalRequiredRoleGroupOtherRolesAsync(List<OrganisationEligibleRole> organisationEligibleRoles) 
    {
      var servicesWithApprovalRequiredRole = await ServiceRoleGroupsWithApprovalRequiredRoleAsync();

      foreach (var approvalRoleService in servicesWithApprovalRequiredRole)
      {
        // Remove all the roles of approval required service except approval required role.
        // All roles of approval required service will be assigned once approval required role is approved.
        var removeRoles = approvalRoleService.CcsServiceRoleMappings.Where(x => x.CcsAccessRole.ApprovalRequired != 1).Select(x => x.CcsAccessRoleId).ToList();
        organisationEligibleRoles.RemoveAll(x => removeRoles.Contains(x.CcsAccessRoleId));
      }
    }

    private async Task<List<int>> OrgRolesToCcsRoles(List<int> roleIds)
    {
      return await _dataContext.OrganisationEligibleRole
        .Where(x => !x.IsDeleted && roleIds.Contains(x.Id))
        .Select(x => x.CcsAccessRoleId)
        .Distinct()
        .ToListAsync();
    }
  }
}
