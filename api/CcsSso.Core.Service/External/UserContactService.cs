using CcsSso.Core.Domain.Contracts.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Service.External
{
  public class UserContactService : IUserContactService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    private readonly IUserProfileHelperService _userHelper;
    public UserContactService(IDataContext dataContext, IContactsHelperService contactsHelper,
      IUserProfileHelperService userHelper)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _userHelper = userHelper;
    }

    /// <summary>
    /// Create a user contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task<int> CreateUserContactAsync(string userName, ContactInfo contactInfo)
    {
      _userHelper.ValidateUserName(userName);

      _contactsHelper.ValidateContacts(contactInfo);

      var user = await _dataContext.User
        .Include(u => u.Party.Person)
        .FirstOrDefaultAsync(u => u.UserName == userName);

      if (user != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactReason);

        #region Create new contact point for person with party
        var partyTypes = await _dataContext.PartyType.Where(pt => pt.PartyTypeName == PartyTypeName.NonUser || pt.PartyTypeName == PartyTypeName.User).ToListAsync();
        var personPartyTypeId = partyTypes.FirstOrDefault(t => t.PartyTypeName == PartyTypeName.NonUser).Id;
        var userPartyTypeId = partyTypes.FirstOrDefault(t => t.PartyTypeName == PartyTypeName.User).Id;

        // Create the contact point with contact details including the person details, having the party type of NON_USER
        var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

        var person = new Person
        {
          FirstName = firstName,
          LastName = lastName,
          OrganisationId = user.Party.Person.OrganisationId
        };

        var party = new Party
        {
          PartyTypeId = personPartyTypeId,
          Person = person,
          ContactPoints = new List<ContactPoint>()
        };

        var contactPoint = new ContactPoint
        {
          Party = party,
          PartyTypeId = personPartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          ContactDetail = new ContactDetail
          {
            EffectiveFrom = DateTime.UtcNow
          }
        };
        await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, contactPoint);

        _dataContext.ContactPoint.Add(contactPoint);
        await _dataContext.SaveChangesAsync();
        #endregion

        #region Add new contact point with created contact details
        // Link the created contact details to user by adding a contact point with a party type of USER
        var userContactPoint = new ContactPoint
        {
          PartyId = user.Party.Id,
          PartyTypeId = userPartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          ContactDetailId = contactPoint.ContactDetailId
        };
        _dataContext.ContactPoint.Add(userContactPoint);
        await _dataContext.SaveChangesAsync();
        #endregion

        return userContactPoint.Id;
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Delete a user contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task DeleteUserContactAsync(string userName, int contactId)
    {
      _userHelper.ValidateUserName(userName);

      var deletingContact = await _dataContext.ContactPoint.Where(c => c.Id == contactId && !c.IsDeleted &&
        c.Party.User.UserName == userName && !c.Party.User.IsDeleted)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .FirstOrDefaultAsync();

      if (deletingContact != null)
      {
        deletingContact.IsDeleted = true;
        deletingContact.ContactDetail.IsDeleted = true;

        deletingContact.ContactDetail.VirtualAddresses.ForEach((virtualAddress) =>
        {
          virtualAddress.IsDeleted = true;
        });

        await _dataContext.SaveChangesAsync();
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Get contact details of a user's contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task<UserContactInfo> GetUserContactAsync(string userName, int contactId)
    {
      _userHelper.ValidateUserName(userName);

      // First get the relevant contact point by id
      var contactPoint = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.Id == contactId && c.Party.User.UserName == userName)
        .FirstOrDefaultAsync();

      if (contactPoint != null)
      {
        // Get the exact contact details with person details, party type (NON_USER)
        var contact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.ContactDetailId == contactPoint.ContactDetailId
          && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
        .Include(c => c.Party.Person).ThenInclude(p => p.Organisation)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail.VirtualAddresses)
        .FirstOrDefaultAsync();

        if (contact != null)
        {

          var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

          var contactInfo = new UserContactInfo
          {
            ContactId = contactPoint.Id,
            UserId = userName,
            ContactReason = contact.ContactPointReason.Name
          };

          _contactsHelper.AssignVirtualContactsToContactResponse(contact, virtualContactTypes, contactInfo);

          if (contact.Party.Person?.Organisation != null)
          {
            contactInfo.OrganisationId = contact.Party.Person.Organisation.CiiOrganisationId;
          }

          return contactInfo;
        }
      }

      throw new ResourceNotFoundException();
    }

    /// <summary>
    /// Get list of contact for a user
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<UserContactInfoList> GetUserContactsListAsync(string userName, string contactType = null)
    {
      _userHelper.ValidateUserName(userName);

      List<ContactResponseInfo> contactInfos = new List<ContactResponseInfo>();

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(prs => prs.Organisation)
        .FirstOrDefaultAsync(u => u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      // Get the user's contact points with party type USER
      var userContactPoints = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.Party.User.UserName == userName && c.PartyType.PartyTypeName == PartyTypeName.User)
        .ToListAsync();

      if (userContactPoints.Any())
      {
        var userContactDetailsIds = userContactPoints.Select(u => u.ContactDetailId).ToList();

        // Get the contact details information for the user's contact points by filtering party type NON_USER
        var contacts = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && userContactDetailsIds.Contains(c.ContactDetailId) && c.PartyType.PartyTypeName == PartyTypeName.NonUser &&
          (contactType == null || c.ContactPointReason.Name == contactType))
        .Include(c => c.Party.Person).ThenInclude(p => p.Organisation)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail.VirtualAddresses)
        .ToListAsync();

        var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

        foreach (var contact in contacts)
        {
          var contactInfo = new ContactResponseInfo
          {
            ContactId = userContactPoints.FirstOrDefault(cp => cp.ContactDetailId == contact.ContactDetailId).Id,
            ContactReason = contact.ContactPointReason.Name
          };

          _contactsHelper.AssignVirtualContactsToContactResponse(contact, virtualContactTypes, contactInfo);

          contactInfos.Add(contactInfo);
        }
      }

      return new UserContactInfoList
      {
        UserId = userName,
        OrganisationId = user.Party.Person.Organisation.CiiOrganisationId,
        ContactsList = contactInfos
      };
    }

    /// <summary>
    /// Update user contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task UpdateUserContactAsync(string userName, int contactId, ContactInfo contactInfo)
    {
      _userHelper.ValidateUserName(userName);

      _contactsHelper.ValidateContacts(contactInfo);

      // Get the user's contact point
      var updatingContactPoint = await _dataContext.ContactPoint
      .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.Id == contactId && c.Party.User.UserName == userName)
      .FirstOrDefaultAsync();

      if (updatingContactPoint != null)
      {

        // Get the relevant contact details and person record for the user's contact points contact details
        var updatingContact = await _dataContext.ContactPoint
          .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.ContactDetailId == updatingContactPoint.ContactDetailId
            && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
          .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
          .Include(c => c.Party).ThenInclude(p => p.Person)
          .FirstOrDefaultAsync();

        if (updatingContact != null)
        {
          var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

          updatingContact.Party.Person.FirstName = firstName;
          updatingContact.Party.Person.LastName = lastName;

          var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactReason);

          updatingContact.ContactPointReasonId = contactPointReasonId;

          await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, updatingContact);

          await _dataContext.SaveChangesAsync();
        }
        else
        {
          throw new ResourceNotFoundException();
        }
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }
  }
}
