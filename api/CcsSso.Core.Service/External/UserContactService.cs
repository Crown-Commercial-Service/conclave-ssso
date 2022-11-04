using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
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
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private IAdaptorNotificationService _adaptorNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly IAuditLoginService _auditLoginService;
    public UserContactService(IDataContext dataContext, IContactsHelperService contactsHelper,
      IUserProfileHelperService userHelper, ICcsSsoEmailService ccsSsoEmailService, IAdaptorNotificationService adaptorNotificationService,
      IWrapperCacheService wrapperCacheService, IAuditLoginService auditLoginService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _userHelper = userHelper;
      _ccsSsoEmailService = ccsSsoEmailService;
      _adaptorNotificationService = adaptorNotificationService;
      _wrapperCacheService = wrapperCacheService;
      _auditLoginService = auditLoginService;
    }

    /// <summary>
    /// Create a user contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task<int> CreateUserContactAsync(string userName, ContactRequestInfo contactInfo)
    {
      _userHelper.ValidateUserName(userName);

      await _contactsHelper.ValidateContactsAsync(contactInfo);

      var user = await _dataContext.User
        .Include(u => u.Party.Person).ThenInclude(p => p.Organisation)
        .FirstOrDefaultAsync(u => u.UserName == userName && !u.IsDeleted);

      if (user != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactPointReason);

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

        var personContactPoint = new ContactPoint
        {
          Party = party,
          PartyTypeId = personPartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          ContactDetail = new ContactDetail
          {
            EffectiveFrom = DateTime.UtcNow
          }
        };
        await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, personContactPoint);

        _dataContext.ContactPoint.Add(personContactPoint);
        #endregion

        #region Add new contact point with created contact details
        // Link the created contact details to user by adding a contact point with a party type of USER
        var userContactPoint = new ContactPoint
        {
          PartyId = user.Party.Id,
          PartyTypeId = userPartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          ContactDetail = personContactPoint.ContactDetail
        };
        _dataContext.ContactPoint.Add(userContactPoint);
        #endregion

        await _dataContext.SaveChangesAsync();

        var contactIds = personContactPoint.ContactDetail.VirtualAddresses != null ? personContactPoint.ContactDetail.VirtualAddresses.Select(va => va.Id).ToList()
          : new List<int>();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.UserContactCreate, AuditLogApplication.ManageMyAccount, $"UserId:{user.Id}, OrgContactPointId:{userContactPoint.Id}, ContactDetailId:{personContactPoint.ContactDetail.Id}" +
          $", OriginalContactPointId:{personContactPoint.Id}, ContactIds:{string.Join(",", contactIds)}" +
          $", RequestContactTypes:{string.Join(",", contactInfo.Contacts.Select(c => c.ContactType))}" +
          $", RequestContactPointReason:{contactInfo.ContactPointReason}");

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.UserContactPoints}-{userName}");

        // Notify Adapter
        await _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.UserContact, OperationType.Create, user.Party.Person.Organisation.CiiOrganisationId, contactIds);

        // Generate email
        await _ccsSsoEmailService.SendUserContactUpdateEmailAsync(userName);

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

      var deletingContactPoint = await _dataContext.ContactPoint.Where(c => c.Id == contactId && !c.IsDeleted &&
        c.Party.User.UserName == userName)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses).ThenInclude(va => va.VirtualAddressType)
        .Include(c => c.ContactPointReason)
        .Include(c => c.Party).ThenInclude(p => p.User)
        .Include(c => c.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
        .FirstOrDefaultAsync();

      if (deletingContactPoint != null)
      {
        deletingContactPoint.IsDeleted = true;
        deletingContactPoint.ContactDetail.IsDeleted = true;

        var deletingVirtualAddresses = deletingContactPoint.ContactDetail.VirtualAddresses;

        deletingVirtualAddresses.ForEach((virtualAddress) =>
        {
          virtualAddress.IsDeleted = true;
        });

        await _dataContext.SaveChangesAsync();

        // Delete assigned contacts
        await _contactsHelper.DeleteAssignedContactsAsync(contactId);

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.UserContactDelete, AuditLogApplication.ManageMyAccount, $"UserId:{deletingContactPoint.Party.User.Id}, UserContactPointId:{contactId}, ContactDetailId:{deletingContactPoint.ContactDetailId}" +
          $", DeletedContactIds:{string.Join(",", deletingVirtualAddresses.Select(va => va.Id))}, DeletedContactTypes:{string.Join(",", deletingVirtualAddresses.Select(va => va.VirtualAddressType.Name))}" +
          $", DeletingContactPointReason:{deletingContactPoint.ContactPointReason.Name}");

        //Invalidate redis
        var invalidatingCacheKeys = new List<string>();
        invalidatingCacheKeys.AddRange(deletingContactPoint.ContactDetail.VirtualAddresses.Select(va => $"{CacheKeyConstant.Contact}-{va.Id}").ToList());
        invalidatingCacheKeys.Add($"{CacheKeyConstant.UserContactPoints}-{userName}");
        await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());

        // Notify Adapter
        var contactIds = deletingContactPoint.ContactDetail.VirtualAddresses.Select(va => va.Id).ToList();
        await _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.UserContact, OperationType.Delete, deletingContactPoint.Party.Person.Organisation.CiiOrganisationId, contactIds);

        // Generate email
        await _ccsSsoEmailService.SendUserContactUpdateEmailAsync(userName);
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
            ContactPointId = contactPoint.Id,
            Detail = new UserDetailInfo
            {
              UserId = userName,
            },
            ContactPointReason = contact.ContactPointReason.Name,
            Contacts = new List<ContactResponseDetail>()
          };

          _contactsHelper.AssignVirtualContactsToContactResponse(contact, virtualContactTypes, contactInfo);

          if (contact.Party.Person?.Organisation != null)
          {
            contactInfo.Detail.OrganisationId = contact.Party.Person.Organisation.CiiOrganisationId;
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
        .FirstOrDefaultAsync(u => u.UserName == userName && !u.IsDeleted);

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
            ContactPointId = userContactPoints.FirstOrDefault(cp => cp.ContactDetailId == contact.ContactDetailId).Id,
            ContactPointReason = contact.ContactPointReason.Name,
            Contacts = new List<ContactResponseDetail>()
          };

          _contactsHelper.AssignVirtualContactsToContactResponse(contact, virtualContactTypes, contactInfo);

          contactInfos.Add(contactInfo);
        }
      }

      return new UserContactInfoList
      {
        Detail = new UserDetailInfo
        {
          UserId = userName,
          OrganisationId = user.Party.Person.Organisation.CiiOrganisationId,
        },
        ContactPoints = contactInfos
      };
    }

    /// <summary>
    /// Update user contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task UpdateUserContactAsync(string userName, int contactId, ContactRequestInfo contactInfo)
    {
      _userHelper.ValidateUserName(userName);

      await _contactsHelper.ValidateContactsAsync(contactInfo);

      // Get the user's contact point
      var updatingContactPoint = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.Id == contactId && c.Party.User.UserName == userName)
        .Include(c => c.Party).ThenInclude(p => p.User)
        .FirstOrDefaultAsync();

      if (updatingContactPoint != null)
      {

        // Get the relevant contact details and person record for the user's contact points contact details
        var updatingContact = await _dataContext.ContactPoint
          .Where(c => !c.IsDeleted && !c.ContactDetail.IsDeleted && c.ContactDetailId == updatingContactPoint.ContactDetailId
            && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
          .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
          .Include(c => c.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
          .FirstOrDefaultAsync();

        if (updatingContact != null)
        {
          var previousVirtualContacts = updatingContact.ContactDetail.VirtualAddresses.Select(va => new KeyValuePair<int, string>(va.Id, va.VirtualAddressValue)).ToList();
          var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

          updatingContact.Party.Person.FirstName = firstName;
          updatingContact.Party.Person.LastName = lastName;

          var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactPointReason);

          updatingContact.ContactPointReasonId = contactPointReasonId;

          await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, updatingContact);

          await _dataContext.SaveChangesAsync();

          var updatedVirtualContacts = updatingContact.ContactDetail.VirtualAddresses.Select(va => new KeyValuePair<int, string>(va.Id, va.VirtualAddressValue)).ToList();

          var createdContactIds = updatedVirtualContacts.Where(uc => !previousVirtualContacts.Any(pc => pc.Key == uc.Key)).Select(uc => uc.Key).ToList();
          var deletedContactIds = previousVirtualContacts.Where(pc => !updatedVirtualContacts.Any(uc => uc.Key == pc.Key)).Select(pc => pc.Key).ToList();
          var updatedContactIds = previousVirtualContacts.Where(pc => updatedVirtualContacts.Any(uc => uc.Key == pc.Key)).Select(pc => pc.Key).ToList();

          // Log
          await _auditLoginService.CreateLogAsync(AuditLogEvent.UserContactUpdate, AuditLogApplication.ManageMyAccount, $"UserId:{updatingContactPoint.Party.User.Id}, UserContactPointId:{contactId}, ContactDetailId:{updatingContact.ContactDetailId}" +
            $", AddedContactIds:{string.Join(",", createdContactIds)}, DeletedContactIds:{string.Join(",", deletedContactIds)}, UpdatedContactIds:{string.Join(",", updatedContactIds)}" +
            $", RequestContactTypes:{string.Join(",", contactInfo.Contacts.Select(c => c.ContactType))}" +
            $", RequestContactPointReason:{contactInfo.ContactPointReason}");

          //Invalidate redis
          var invalidatingCacheKeys = new List<string>();
          deletedContactIds.ForEach((id) => invalidatingCacheKeys.Add($"{CacheKeyConstant.Contact}-{id}"));
          updatedContactIds.ForEach((id) => invalidatingCacheKeys.Add($"{CacheKeyConstant.Contact}-{id}"));
          invalidatingCacheKeys.Add($"{CacheKeyConstant.UserContactPoints}-{userName}");
          await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());

          // Notify Adapter
          var ciiOrganisationId = updatingContact.Party.Person.Organisation.CiiOrganisationId;
          var createdContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.UserContact, OperationType.Create, ciiOrganisationId, createdContactIds);
          var deletedContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.UserContact, OperationType.Delete, ciiOrganisationId, deletedContactIds);
          var updatedContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.UserContact, OperationType.Update, ciiOrganisationId, updatedContactIds);

          await Task.WhenAll(createdContactNotifyTask, deletedContactNotifyTask, updatedContactNotifyTask);

          // Generate email
          await _ccsSsoEmailService.SendUserContactUpdateEmailAsync(userName);
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
