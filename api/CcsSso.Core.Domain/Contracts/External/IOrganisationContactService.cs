using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts.External
{
  public interface IOrganisationContactService
  {

    Task<List<int>> AssignContactsToOrganisationAsync(string ciiOrganisationId, ContactAssignmentInfo contactAssignmentInfo);

    Task<int> CreateOrganisationContactAsync(string ciiOrganisationId, ContactRequestInfo contactInfo);

    Task DeleteOrganisationContactAsync(string ciiOrganisationId, int contactId);

    Task<OrganisationContactInfoList> GetOrganisationContactsListAsync(string ciiOrganisationId, string contactType = null, ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All);

    Task<OrganisationContactInfo> GetOrganisationContactAsync(string ciiOrganisationId, int contactId);

    Task UnassignOrganisationContactsAsync(string ciiOrganisationId, List<int> unassigningContactPointIds);

    Task UpdateOrganisationContactAsync(string ciiOrganisationId, int contactId, ContactRequestInfo contactInfo);
  }
}
