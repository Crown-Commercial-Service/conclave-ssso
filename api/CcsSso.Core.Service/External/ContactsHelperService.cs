using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Service.External
{
  public class ContactsHelperService : IContactsHelperService
  {
    private readonly IDataContext _dataContext;
    public ContactsHelperService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    /// <summary>
    /// Get the in memory contact point object to use in both create and update methods
    /// </summary>
    /// <param name="contactInfo"></param>
    /// <param name="contactPoint"></param>
    /// <returns></returns>
    public async Task AssignVirtualContactsToContactPointAsync(ContactRequestInfo contactInfo, ContactPoint contactPoint)
    {
      List<VirtualAddress> updatingVirtualAddresses = await GetVirtualAddressesAsync(contactInfo);

      if (updatingVirtualAddresses.Any())
      {
        if (contactPoint.ContactDetail.VirtualAddresses != null)
        {
          contactPoint.ContactDetail.VirtualAddresses.RemoveAll(va => !updatingVirtualAddresses.Any(uva => uva.VirtualAddressTypeId == va.VirtualAddressTypeId));
          updatingVirtualAddresses.ForEach((virtualAddress) =>
          {
            var existingVirtualAddress = contactPoint.ContactDetail.VirtualAddresses
              .FirstOrDefault(a => a.VirtualAddressTypeId == virtualAddress.VirtualAddressTypeId);
            if (existingVirtualAddress != null)
            {
              existingVirtualAddress.VirtualAddressValue = virtualAddress.VirtualAddressValue;
            }
            else
            {
              contactPoint.ContactDetail.VirtualAddresses.Add(virtualAddress);
            }
          });
        }
        else
        {
          contactPoint.ContactDetail.VirtualAddresses = updatingVirtualAddresses;
        }
      }
      else
      {
        if (contactPoint.ContactDetail.VirtualAddresses != null)
        {
          contactPoint.ContactDetail.VirtualAddresses.RemoveAll(va => true);
        }
      }
    }

    /// <summary>
    /// Assign the virtual contact details to contact response object
    /// </summary>
    /// <param name="virtualAddresses"></param>
    /// <param name="virtualContactTypes"></param>
    /// <param name="contactResponseInfo"></param>
    public void AssignVirtualContactsToContactResponse(ContactPoint contactPoint, List<VirtualAddressType> virtualContactTypes,
      ContactResponseInfo contactResponseInfo)
    {
      var virtualAddresses = contactPoint.ContactDetail.VirtualAddresses;
      if (virtualAddresses != null)
      {
        foreach (var virtualAddress in virtualAddresses)
        {
          if (!virtualAddress.IsDeleted)
          {
            var contactType = virtualContactTypes.First(t => t.Id == virtualAddress.VirtualAddressTypeId).Name;
            ContactResponseDetail contactResponseDetail = new ContactResponseDetail
            {
              ContactId = virtualAddress.Id,
              ContactType = contactType,
              ContactValue = virtualAddress.VirtualAddressValue
            };
            contactResponseInfo.Contacts.Add(contactResponseDetail); 
          }
        }
      }

      if (contactPoint.Party.Person != null)
      {
        contactResponseInfo.ContactPointName = $"{contactPoint.Party.Person.FirstName} {contactPoint.Party.Person.LastName}";
      }
    }

    /// <summary>
    /// Get first name and last name for a contact person from contact name, value list
    /// </summary>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public (string firstName, string lastName) GetContactPersonNameTuple(ContactRequestInfo contactInfo)
    {
      var firstName = string.Empty;
      var lastName = string.Empty;
      var name = contactInfo.ContactPointName;
      if (!string.IsNullOrWhiteSpace(name))
      {
        var nameArray = name.Trim().Split(" ");
        firstName = nameArray[0];
        lastName = nameArray.Length >= 2 ? nameArray[nameArray.Length - 1] : string.Empty;
      }
      return (firstName, lastName);
    }

    /// <summary>
    /// Get the contact point reason id for the reason provided.
    /// If not return the id for OTHER reason option.
    /// </summary>
    /// <param name="reason"></param>
    /// <returns></returns>
    public async Task<int> GetContactPointReasonIdAsync(string reason)
    {
      var reasonString = string.IsNullOrWhiteSpace(reason) ? ContactReasonType.Unspecified : reason.ToUpper();

      var contactPointReason = await _dataContext.ContactPointReason.FirstOrDefaultAsync(r => r.Name == reasonString);

      if (contactPointReason != null)
      {
        return contactPointReason.Id;
      }
      else
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactReason);
      }
    }

    public async Task<List<ContactReasonInfo>> GetContactPointReasonsAsync()
    {
      var excludingContactReasons = new List<string> { "OTHER", "SITE", "UNSPECIFIED" };
      var contactPointReasons = await _dataContext.ContactPointReason.Where(r => !excludingContactReasons.Contains(r.Name)).OrderBy(r => r.Name).ToListAsync();

      var contactReasonInfoList = contactPointReasons.Select(r => new ContactReasonInfo { Key = r.Name, Value = r.Description }).ToList();

      return contactReasonInfoList;
    }

    public async Task<List<string>> GetContactTypesAsync()
    {
      var contactTypes = await _dataContext.VirtualAddressType.Select(t => t.Name).ToListAsync();

      return contactTypes;
    }

    /// <summary>
    /// Validate the contact details
    /// </summary>
    /// <param name="contactInfo"></param>
    public async Task ValidateContactsAsync(ContactRequestInfo contactInfo)
    {
      var validContactsNameList = await _dataContext.VirtualAddressType.Select(vat => vat.Name).ToListAsync();

      if (string.IsNullOrWhiteSpace(contactInfo.ContactPointName) &&
        (contactInfo.Contacts == null || !contactInfo.Contacts.Any() || !contactInfo.Contacts.Any(c => !string.IsNullOrWhiteSpace(c.ContactValue))))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      }

      if (contactInfo.Contacts.Any(c => string.IsNullOrWhiteSpace(c.ContactType)) ||
        contactInfo.Contacts.Any(c => !validContactsNameList.Contains(c.ContactType)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactType);
      }

      var email = contactInfo.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Email)?.ContactValue;

      if (!string.IsNullOrEmpty(email) && !UtilitiesHelper.IsEmailValid(email))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidEmail);
      }

      var phoneNumber = contactInfo.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Phone)?.ContactValue;

      // Validate the phone number for the E.164 standard
      if (!string.IsNullOrEmpty(phoneNumber) && !UtilitiesHelper.IsPhoneNumberValid(phoneNumber))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidPhoneNumber);
      }
    }


    /// <summary>
    /// Get the virtual addresses entities for contacts
    /// </summary>
    /// <param name="contacts"></param>
    /// <returns></returns>
    private async Task<List<VirtualAddress>> GetVirtualAddressesAsync(ContactRequestInfo contactInfo)
    {
      var virtualAddressTypes = await _dataContext.VirtualAddressType.ToListAsync();

      List<VirtualAddress> virtualAddresses = new List<VirtualAddress>();

      foreach (var contact in contactInfo.Contacts)
      {
        if (!string.IsNullOrWhiteSpace(contact.ContactValue))
        {
          var virtualAddress = new VirtualAddress
          {
            VirtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == contact.ContactType).Id,
            VirtualAddressValue = contact.ContactValue
          };
          virtualAddresses.Add(virtualAddress);
        }
      }
      return virtualAddresses;
    }
  }
}
