using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface ICacheInvalidateService
  {
    Task RemoveUserCacheValuesOnDeleteAsync(string userName, string organisationId, List<int> contactPointIds);

    Task RemoveOrganisationCacheValuesOnDeleteAsync(string ciiOrganisationId, List<int> contactPointIds, Dictionary<string,List<int>> siteContactPoints);
  }
}
