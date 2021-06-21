using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts.External
{
  public interface IOrganisationContactService
  {
    Task<int> CreateOrganisationContactAsync(string ciiOrganisationId, ContactRequestInfo contactInfo);

    Task DeleteOrganisationContactAsync(string ciiOrganisationId, int contactId);

    Task<OrganisationContactInfoList> GetOrganisationContactsListAsync(string ciiOrganisationId, string contactType = null);

    Task<OrganisationContactInfo> GetOrganisationContactAsync(string ciiOrganisationId, int contactId);

    Task UpdateOrganisationContactAsync(string ciiOrganisationId, int contactId, ContactRequestInfo contactInfo);
  }
}
