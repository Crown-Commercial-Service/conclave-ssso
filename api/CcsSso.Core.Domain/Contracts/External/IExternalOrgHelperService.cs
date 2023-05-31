using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IExternalOrgHelperService
  {
    Task<int> GetOrganisationAdminAccessRoleId(int organisationId);
  }
}
