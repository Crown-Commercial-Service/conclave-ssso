using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts
{
  public interface IOrganisationService
  {
    Task<Dictionary<string, object>> GetOrganisationAsync(string organisationId);
  }
}
