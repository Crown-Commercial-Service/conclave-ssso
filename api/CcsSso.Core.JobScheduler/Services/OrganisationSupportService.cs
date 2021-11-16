using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Services
{
  public class OrganisationSupportService : IOrganisationSupportService
  {
    private IDataContext _dataContext;
    public OrganisationSupportService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }
    public async Task<List<User>> GetAdminUsersAsync(int organisationId)
    {
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
                                 .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId &&
                                 or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;

      var orgAdmins = await _dataContext.User.Where(u => !u.IsDeleted && u.AccountVerified
                     && u.Party.Person.OrganisationId == organisationId
                     && (u.UserGroupMemberships.Any(ugm => !ugm.IsDeleted
                     && ugm.OrganisationUserGroup.GroupEligibleRoles.Any(ga => !ga.IsDeleted && ga.OrganisationEligibleRoleId == orgAdminAccessRoleId))
                     || u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId)))
                    .Select(u => u).OrderBy(u => u.Id).ToListAsync();

      return orgAdmins;
    }
  }
}
