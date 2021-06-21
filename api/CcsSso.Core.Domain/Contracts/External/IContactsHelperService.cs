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

    (string firstName, string lastName) GetContactPersonNameTuple(ContactRequestInfo contactInfo);

    Task<int> GetContactPointReasonIdAsync(string reason);

    Task<List<ContactReasonInfo>> GetContactPointReasonsAsync();

    Task<List<string>> GetContactTypesAsync();

    Task ValidateContactsAsync(ContactRequestInfo contactInfo);
  }
}
