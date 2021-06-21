using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts
{
  public interface IContactService
  {
    Task<int> CreateContactAsync(Dictionary<string, object> contactData);

    Task<int> UpdateContactAsync(int contactId, Dictionary<string, object> contactData);

    Task<Dictionary<string, object>> GetContactAsync(int contactId);

    Task<int> CreateUserContactAsync(string userName, Dictionary<string, object> contactData);

    Task<int> UpdateUserContactAsync(int contactId, string userName, Dictionary<string, object> contactData);

    Task<Dictionary<string, object>> GetUserContactAsync(int contactId, string userName);

    Task<int> CreateOrganisationContactAsync(string organisationId, Dictionary<string, object> contactData);

    Task<int> UpdateOrganisationContactAsync(int contactId, string organisationId, Dictionary<string, object> contactData);

    Task<Dictionary<string, object>> GetOrganisationContactAsync(int contactId, string organisationId);

    Task<int> CreateSiteContactAsync(string organisationId, int siteId, Dictionary<string, object> contactData);

    Task<int> UpdateSiteContactAsync(int contactId, string organisationId, int siteId, Dictionary<string, object> contactData);

    Task<Dictionary<string, object>> GetSiteContactAsync(int contactId, string organisationId, int siteId);
  }
}
