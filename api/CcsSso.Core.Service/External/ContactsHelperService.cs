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
    public async Task AssignVirtualContactsToContactPointAsync(ContactInfo contactInfo, ContactPoint contactPoint)
    {
      List<VirtualAddress> virtualAddresses = await GetVirtualAddressesAsync(contactInfo);

      if (virtualAddresses.Any())
      {
        if (contactPoint.ContactDetail.VirtualAddresses != null)
        {
          contactPoint.ContactDetail.VirtualAddresses.RemoveAll(va => true);
          contactPoint.ContactDetail.VirtualAddresses.AddRange(virtualAddresses);
        }
        else
        {
          contactPoint.ContactDetail.VirtualAddresses = virtualAddresses;
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
        contactResponseInfo.Email = virtualAddresses.FirstOrDefault(va => va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct =>
          vct.Name == VirtualContactTypeName.Email)?.Id)?.VirtualAddressValue ?? string.Empty;

        contactResponseInfo.PhoneNumber = virtualAddresses.FirstOrDefault(va => va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct =>
          vct.Name == VirtualContactTypeName.Phone)?.Id)?.VirtualAddressValue ?? string.Empty;

        contactResponseInfo.Fax = virtualAddresses.FirstOrDefault(va => va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct =>
          vct.Name == VirtualContactTypeName.Fax)?.Id)?.VirtualAddressValue ?? string.Empty;

        contactResponseInfo.WebUrl = virtualAddresses.FirstOrDefault(va => va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct =>
           vct.Name == VirtualContactTypeName.Url)?.Id)?.VirtualAddressValue ?? string.Empty;
      }

      if (contactPoint.Party.Person != null)
      {
        contactResponseInfo.Name = $"{contactPoint.Party.Person.FirstName} {contactPoint.Party.Person.LastName}";
      }
    }

    /// <summary>
    /// Get first name and last name for a contact person from contact name, value list
    /// </summary>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public (string firstName, string lastName) GetContactPersonNameTuple(ContactInfo contactInfo)
    {
      var firstName = string.Empty;
      var lastName = string.Empty;
      var name = contactInfo.Name;
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

    public async Task<List<ContactReasonInfo>> GetContactPointReasonsForUIAsync()
    {
      var excludingContactReasons = new List<string> { "OTHER", "SITE", "UNSPECIFIED" };
      var contactPointReasons = await _dataContext.ContactPointReason.Where(r => !excludingContactReasons.Contains(r.Name)).OrderBy(r => r.Name).ToListAsync();

      var contactReasonInfoList = contactPointReasons.Select(r => new ContactReasonInfo { Key= r.Name, Value = r.Description }).ToList();

      return contactReasonInfoList;
    }

    /// <summary>
    /// Validate the contact details
    /// </summary>
    /// <param name="contactInfo"></param>
    public void ValidateContacts(ContactInfo contactInfo)
    {
      if (string.IsNullOrWhiteSpace(contactInfo.Name) && string.IsNullOrWhiteSpace(contactInfo.Email)
        && string.IsNullOrWhiteSpace(contactInfo.PhoneNumber) && string.IsNullOrWhiteSpace(contactInfo.Fax)
        && string.IsNullOrWhiteSpace(contactInfo.WebUrl))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      }

      if (!string.IsNullOrWhiteSpace(contactInfo.Email) && !UtilitiesHelper.IsEmailValid(contactInfo.Email))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidEmail);
      }

      // Validate the phone number for the E.164 standard
      if (!string.IsNullOrWhiteSpace(contactInfo.PhoneNumber) && !UtilitiesHelper.IsPhoneNumberValid(contactInfo.PhoneNumber))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidPhoneNumber);
      }
    }


    /// <summary>
    /// Get the virtual addresses entities for contacts
    /// </summary>
    /// <param name="contacts"></param>
    /// <returns></returns>
    private async Task<List<VirtualAddress>> GetVirtualAddressesAsync(ContactInfo contactInfo)
    {
      var virtualAddressTypes = await _dataContext.VirtualAddressType.ToListAsync();

      List<VirtualAddress> virtualAddresses = new List<VirtualAddress>();

      if (!string.IsNullOrWhiteSpace(contactInfo.Email))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Email).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactInfo.Email
        };
        virtualAddresses.Add(virtualAddress);
      }
      if (!string.IsNullOrWhiteSpace(contactInfo.PhoneNumber))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Phone).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactInfo.PhoneNumber
        };
        virtualAddresses.Add(virtualAddress);
      }
      if (!string.IsNullOrWhiteSpace(contactInfo.Fax))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Fax).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactInfo.Fax
        };
        virtualAddresses.Add(virtualAddress);
      }
      if (!string.IsNullOrWhiteSpace(contactInfo.WebUrl))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Url).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactInfo.WebUrl
        };
        virtualAddresses.Add(virtualAddress);
      }
      return virtualAddresses;
    }
  }
}
