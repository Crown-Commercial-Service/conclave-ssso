using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationSiteContactService
  {
    Task<List<int>> AssignContactsToSiteAsync(string ciiOrganisationId, int siteId, ContactAssignmentInfo contactAssignmentInfo);

    Task<int> CreateOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, ContactRequestInfo contactInfo);

    Task DeleteOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId);

    Task<OrganisationSiteContactInfoList> GetOrganisationSiteContactsListAsync(string ciiOrganisationId, int siteId, string contactType = null, ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All);

    Task<OrganisationSiteContactInfo> GetOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId);

    Task UnassignSiteContactsAsync(string ciiOrganisationId, int siteId, List<int> unassigningContactPointIds);

    Task UpdateOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId, ContactRequestInfo contactInfo);
  }
}
