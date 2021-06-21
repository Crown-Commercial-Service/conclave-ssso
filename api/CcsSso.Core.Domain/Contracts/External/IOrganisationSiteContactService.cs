using CcsSso.Domain.Dtos.External;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationSiteContactService
  {
    Task<int> CreateOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, ContactRequestInfo contactInfo);

    Task DeleteOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId);

    Task<OrganisationSiteContactInfoList> GetOrganisationSiteContactsListAsync(string ciiOrganisationId, int siteId, string contactType = null);

    Task<OrganisationSiteContactInfo> GetOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId);

    Task UpdateOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId, ContactRequestInfo contactInfo);
  }
}
