using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Service.External
{
  public class ContactsHelperService : IContactsHelperService
  {
    private readonly IDataContext _dataContext;
    private readonly ILocalCacheService _localCacheService;
    public ContactsHelperService(IDataContext dataContext, ILocalCacheService localCacheService)
    {
      _dataContext = dataContext;
      _localCacheService = localCacheService;
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
        contactResponseInfo.ContactPointName = $"{contactPoint.Party.Person.FirstName} {contactPoint.Party.Person.LastName}".Trim();
      }
    }

    /// <summary>
    /// Check for existency of assignable site contact points
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="contactPointIds"></param>
    /// <returns></returns>
    public async Task CheckAssignableSiteContactPointsExistenceAsync(string organisationId, int siteId, List<int> contactPointIds)
    {
      contactPointIds = contactPointIds.Distinct().ToList();

      // Site (which is also a contact point)
      var organisationSiteId = await _dataContext.ContactPoint
        .Where(cp => !cp.IsDeleted && cp.IsSite && cp.Id == siteId && cp.Party.Organisation.CiiOrganisationId == organisationId)
        .Select(cp => cp.Id)
        .FirstOrDefaultAsync();

      // Check whether there is a site, organisation with given ids.
      if (organisationSiteId == 0)
      {
        throw new ResourceNotFoundException();
      }

      // Get site assignable(original) contacts Ids
      var siteContactIds = await _dataContext.SiteContact
        .Where(c => !c.IsDeleted && c.ContactPointId == siteId && c.AssignedContactType == AssignedContactType.None).Select(c => c.Id).ToListAsync();

      var invalidContactIds = contactPointIds.Where(id => !siteContactIds.Contains(id));

      // Has done this way rather than directly checking the existance in DB, since in future there will be a change to the error messages and
      // there may be an option to provide the incorrect ids
      if (invalidContactIds.Any())
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidAssigningContactIds);
      }
    }

    /// <summary>
    /// Check for existency of assignable user contact points
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="userName"></param>
    /// <param name="contactPointIds"></param>
    /// <returns></returns>
    public async Task CheckAssignableUserContactPointsExistenceAsync(string organisationId, string userName, List<int> contactPointIds)
    {
      contactPointIds = contactPointIds.Distinct().ToList();

      var userId = await _dataContext.User.Where(u => !u.IsDeleted && u.UserName == userName && u.Party.Person.Organisation.CiiOrganisationId == organisationId)
        .Select(u => u.Id)
        .FirstOrDefaultAsync();

      if (userId == 0)
      {
        throw new ResourceNotFoundException();
      }

      // Get the user's assignable(orginal contacts) contact points with party type USER
      var userContactPointIds = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.Party.User.UserName == userName
          && c.AssignedContactType == AssignedContactType.None && c.PartyType.PartyTypeName == PartyTypeName.User)
        .Select(c => c.Id)
        .ToListAsync();

      var invalidContactIds = contactPointIds.Where(id => !userContactPointIds.Contains(id));

      // Has done this way rather than directly checking the existence in DB, since in future there will be a change to the error messages and
      // there may be an option to provide the incorrect ids
      if (invalidContactIds.Any())
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidAssigningContactIds);
      }
    }

    /// <summary>
    /// Delete assigned contacts (when deleting the original contact)
    /// </summary>
    /// <param name="contactPointId"></param>
    /// <returns></returns>
    public async Task DeleteAssignedContactsAsync(int contactPointId)
    {
      var assignedToContactPoints = await _dataContext.ContactPoint.Where(cp => cp.OriginalContactPointId == contactPointId).ToListAsync();
      var assignedToSiteContacts = await _dataContext.SiteContact.Where(sc => sc.OriginalContactId == contactPointId).ToListAsync();

      assignedToContactPoints.ForEach((contactPoint) => contactPoint.IsDeleted = true);
      assignedToSiteContacts.ForEach((siteContact) => siteContact.IsDeleted = true);

      await _dataContext.SaveChangesAsync();
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
        lastName = string.Join(" ",nameArray.Skip(1).ToArray());
      }
      return (firstName, lastName);
    }

    /// <summary>
    /// Get all the contact points for UI
    /// Exclude the internally used reasons
    /// </summary>
    /// <returns></returns>
    public async Task<List<ContactReasonInfo>> GetContactPointReasonsAsync()
    {
      var excludingContactReasons = new List<string> { "OTHER", "SITE", "UNSPECIFIED" };

      var contactPointReasons = await GetContactPointReasonsListAsync();
      var contactReasonInfoList = contactPointReasons.Where(r => !excludingContactReasons.Contains(r.Name)).OrderBy(r => r.Name)
        .Select(r => new ContactReasonInfo { Key = r.Name, Value = r.Description }).ToList();

      return contactReasonInfoList;
    }

    /// <summary>
    /// Get the contact point reason id for the reason provided.
    /// If reason null returns the id for UNSPECIFIED reason id.
    /// </summary>
    /// <param name="reason"></param>
    /// <returns></returns>
    public async Task<int> GetContactPointReasonIdAsync(string reason)
    {
      var reasonString = string.IsNullOrWhiteSpace(reason) ? ContactReasonType.Unspecified : reason.ToUpper();

      var contactPointReasons = await GetContactPointReasonsListAsync();
      var contactPointReason = contactPointReasons.FirstOrDefault(r => r.Name == reasonString);

      if (contactPointReason != null)
      {
        return contactPointReason.Id;
      }
      else
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactReason);
      }
    }

    /// <summary>
    /// Get contact point reason by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<string> GetContactPointReasonNameAsync(int id)
    {
      var contactPointReasons = await GetContactPointReasonsListAsync();
      var contactPointReason = contactPointReasons.FirstOrDefault(r => r.Id == id);
      return contactPointReason?.Name;
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

      if ((contactInfo.Contacts == null || !contactInfo.Contacts.Any()) && (string.IsNullOrWhiteSpace(contactInfo.ContactPointName)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      }

      if (string.IsNullOrWhiteSpace(contactInfo.ContactPointName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorContactNameRequired);
      }

      //All other special characters not specified in accepted. min 3 max 256
      if (!UtilityHelper.IsContactPointNameValid(contactInfo.ContactPointName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactPointName);
      }

      if (contactInfo.Contacts == null || !contactInfo.Contacts.Any() || !contactInfo.Contacts.Any(c => !string.IsNullOrWhiteSpace(c.ContactValue)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorContactsRequired);
      }

      if (contactInfo.Contacts.Any(c => string.IsNullOrWhiteSpace(c.ContactType)) ||
        contactInfo.Contacts.Any(c => !validContactsNameList.Contains(c.ContactType)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactType);
      }

      var email = contactInfo.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Email)?.ContactValue;

      if (!string.IsNullOrEmpty(email))
      {
        if (!UtilityHelper.IsEmailFormatValid(email))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidEmail);

        }
        if (!UtilityHelper.IsEmailLengthValid(email))
        {
          throw new CcsSsoException(ErrorConstant.ErrorEmailTooLong);
        }
      }

      var phoneNumber = contactInfo.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Phone)?.ContactValue;

      // Validate the phone number for the E.164 standard
      if (!string.IsNullOrEmpty(phoneNumber) && !UtilityHelper.IsPhoneNumberValid(phoneNumber))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidPhoneNumber);
      }

      var faxNumber = contactInfo.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Fax)?.ContactValue;

      // Validate the fax number for the E.164 standard
      if (!string.IsNullOrEmpty(faxNumber) && !UtilityHelper.IsPhoneNumberValid(faxNumber))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidFaxNumber);
      }

      var mobileNumber = contactInfo.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Mobile)?.ContactValue;

      // Validate the mobile number for the E.164 standard
      if (!string.IsNullOrEmpty(mobileNumber) && !UtilityHelper.IsPhoneNumberValid(mobileNumber))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidMobileNumber);
      }
    }

    /// <summary>
    /// Validate for contact assginements
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="contactAssignmentInfo"></param>
    /// <returns></returns>
    public async Task ValidateContactAssignmentAsync(string organisationId, ContactAssignmentInfo contactAssignmentInfo, List<AssignedContactType> allowedContactTypes)
    {
      if (contactAssignmentInfo.AssigningContactPointIds == null || !contactAssignmentInfo.AssigningContactPointIds.Any())
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidAssigningContactIds);
      }

      if (!allowedContactTypes.Contains(contactAssignmentInfo.AssigningContactType))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactAssignmentType);
      }

      switch (contactAssignmentInfo.AssigningContactType)
      {
        case AssignedContactType.User:
          if (string.IsNullOrWhiteSpace(contactAssignmentInfo.AssigningContactsUserId))
          {
            throw new CcsSsoException(ErrorConstant.ErrorInvalidUserIdForContactAssignment);
          }
          await CheckAssignableUserContactPointsExistenceAsync(organisationId, contactAssignmentInfo.AssigningContactsUserId, contactAssignmentInfo.AssigningContactPointIds);
          break;
        case AssignedContactType.Site:
          if (contactAssignmentInfo.AssigningContactsSiteId == null || contactAssignmentInfo.AssigningContactsSiteId == 0)
          {
            throw new CcsSsoException(ErrorConstant.ErrorInvalidSiteIdForContactAssignment);
          }
          await CheckAssignableSiteContactPointsExistenceAsync(organisationId, contactAssignmentInfo.AssigningContactsSiteId ?? 0, contactAssignmentInfo.AssigningContactPointIds);
          break;
        default:
          throw new CcsSsoException(ErrorConstant.ErrorInvalidContactAssignmentType);
      }
    }

    /// <summary>
    /// Get all the contact points in the db with inmemory cache
    /// </summary>
    /// <returns></returns>
    private async Task<List<ContactPointReason>> GetContactPointReasonsListAsync()
    {
      var contactPointReasons = await _localCacheService.GetOrSetValueAsync<List<ContactPointReason>>("CONTACT_POINT_REASONS", async () =>
      {
        return await _dataContext.ContactPointReason.ToListAsync();
      }, 30);

      return contactPointReasons;
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
