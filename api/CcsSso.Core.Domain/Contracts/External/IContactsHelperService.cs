using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts.External
{
  public interface IContactsHelperService
  {
    Task AssignVirtualContactsToContactPointAsync(ContactRequestInfo contactInfo, ContactPoint contactPoint);

    void AssignVirtualContactsToContactResponse(ContactPoint contactPoint, List<VirtualAddressType> virtualContactTypes,
      ContactResponseInfo contactResponseInfo);

    Task CheckAssignableSiteContactPointsExistenceAsync(string organisationId, int siteId, List<int> contactPointIds);

    Task CheckAssignableUserContactPointsExistenceAsync(string organisationId, string userName, List<int> contactPointIds);

    Task DeleteAssignedContactsAsync(int contactPointId);

    (string firstName, string lastName) GetContactPersonNameTuple(ContactRequestInfo contactInfo);

    Task<List<ContactReasonInfo>> GetContactPointReasonsAsync();

    Task<int> GetContactPointReasonIdAsync(string reason);

    Task<string> GetContactPointReasonNameAsync(int id);

    Task<List<string>> GetContactTypesAsync();

    Task ValidateContactsAsync(ContactRequestInfo contactInfo);

    Task ValidateContactAssignmentAsync(string organisationId, ContactAssignmentInfo contactAssignmentInfo, List<AssignedContactType> allowedContactTypes);
  }
}
