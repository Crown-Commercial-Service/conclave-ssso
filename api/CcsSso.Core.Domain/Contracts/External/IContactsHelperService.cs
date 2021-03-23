using CcsSso.DbModel.Entity;
using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts.External
{
  public interface IContactsHelperService
  {
    Task AssignVirtualContactsToContactPointAsync(ContactInfo contactInfo, ContactPoint contactPoint);

    void AssignVirtualContactsToContactResponse(ContactPoint contactPoint, List<VirtualAddressType> virtualContactTypes,
      ContactResponseInfo contactResponseInfo);

    (string firstName, string lastName) GetContactPersonNameTuple(ContactInfo contactInfo);

    Task<int> GetContactPointReasonIdAsync(string reason);

    Task<List<ContactReasonInfo>> GetContactPointReasonsForUIAsync();

    void ValidateContacts(ContactInfo contactInfo);
  }
}
