using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IContactSupportService
  {
    Task<bool> IsOrgSiteContactExistsAsync(List<int> userContacPointIds, int organisationId);

    Task<bool> IsOtherUserContactExistsAsync(string userName, int organisationId);
  }
}
