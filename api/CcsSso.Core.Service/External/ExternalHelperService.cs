using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class ExternalHelperService : IExternalHelperService
  {
    private readonly IDataContext _dataContext;
    public ExternalHelperService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task<List<OrganisationRole>> GetCcsAccessRoles()
    {
      var allRoles = await _dataContext.CcsAccessRole.Where(r => !r.IsDeleted)
                          .Include(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                          .Select(i => new OrganisationRole
                          {
                            RoleId = i.Id,
                            RoleName = i.CcsAccessRoleName,
                            RoleKey = i.CcsAccessRoleNameKey,
                            ServiceName = i.ServiceRolePermissions.FirstOrDefault().ServicePermission.CcsService.ServiceName,
                          }).ToListAsync();
      return allRoles;
    }
    public async Task<int> GetOrganisationAdminAccessRoleId(int organisationId)
    {
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
          .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId
           && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;
      return orgAdminAccessRoleId;
    }

  }
}
