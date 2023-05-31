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
  public class ExternalOrgHelperService : IExternalOrgHelperService
  {
    private readonly IDataContext _dataContext;
    public ExternalOrgHelperService(IDataContext dataContext)
    {
      _dataContext = dataContext;
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
