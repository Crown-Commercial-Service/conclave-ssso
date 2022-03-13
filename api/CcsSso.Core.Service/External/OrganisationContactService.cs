using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.External;
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
  public class OrganisationContactService : IOrganisationContactService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    private IAdaptorNotificationService _adaptorNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly IAuditLoginService _auditLoginService;

    public OrganisationContactService(IDataContext dataContext, IContactsHelperService contactsHelper, IAdaptorNotificationService adaptorNotificationService,
      IWrapperCacheService wrapperCacheService, IAuditLoginService auditLoginService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _adaptorNotificationService = adaptorNotificationService;
      _wrapperCacheService = wrapperCacheService;
      _auditLoginService = auditLoginService;
    }

    /// <summary>
    /// Asssign user or site contacts to an organisation
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactAssignmentInfo"></param>
    /// <returns></returns>
    public async Task<List<int>> AssignContactsToOrganisationAsync(string ciiOrganisationId, ContactAssignmentInfo contactAssignmentInfo)
    {
      await _contactsHelper.ValidateContactAssignmentAsync(ciiOrganisationId, contactAssignmentInfo, new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site });

      var organisation = await _dataContext.Organisation
        .Where(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId)
        .Select(o => new { o.Id, o.PartyId, o.Party.PartyTypeId, o.Party.ContactPoints })
        .FirstOrDefaultAsync();

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      List<ContactPoint> duplicateAssignments = new();

      // When assigning site contacts to organisation request contains only the site contact ids (not the actual contactPointIds of site contacts)
      // This is because, the contact point id of site is not actually a contact point id it is the site contact (table) id eventhough it was refered as contact point id
      // Therefore, in order to figure out the actual contact point for the assigning site contacts 
      if (contactAssignmentInfo.AssigningContactType == AssignedContactType.Site)
      {
        var contactPointIdsOfSiteContacts = await _dataContext.SiteContact.Where(c => !c.IsDeleted && c.ContactPointId == contactAssignmentInfo.AssigningContactsSiteId
          && c.AssignedContactType == AssignedContactType.None && contactAssignmentInfo.AssigningContactPointIds.Contains(c.Id))
          .Select(c => c.ContactId)
          .ToListAsync();
        duplicateAssignments = organisation.ContactPoints.Where(cp => !cp.IsDeleted &&
          contactPointIdsOfSiteContacts.Contains(cp.OriginalContactPointId)).ToList();
        contactAssignmentInfo.AssigningContactPointIds = contactPointIdsOfSiteContacts; // Override the assigning contact point ids with actual site contact point ids
      }
      else
      {
        duplicateAssignments = organisation.ContactPoints
          .Where(cp => !cp.IsDeleted && contactAssignmentInfo.AssigningContactPointIds.Contains(cp.OriginalContactPointId)).ToList();
      }

      // For future error messages duplicated ids may required
      if (duplicateAssignments.Any())
      {
        throw new CcsSsoException(ErrorConstant.ErrorDuplicateContactAssignment);
      }

      var originalContactPointsForAssignment = await _dataContext.ContactPoint
        .Where(cp => !cp.IsDeleted && contactAssignmentInfo.AssigningContactPointIds.Contains(cp.Id))
        .Select(cp => new { cp.Id, cp.ContactPointReasonId, cp.ContactDetailId })
        .ToListAsync();

      List<ContactPoint> assigningContactPoints = new();

      originalContactPointsForAssignment.ForEach((contactPointForAssignment) =>
      {
        ContactPoint newContactPoint = new()
        {
          PartyId = organisation.PartyId,
          PartyTypeId = organisation.PartyTypeId,
          ContactPointReasonId = contactPointForAssignment.ContactPointReasonId,
          ContactDetailId = contactPointForAssignment.ContactDetailId,
          OriginalContactPointId = contactPointForAssignment.Id,
          AssignedContactType = contactAssignmentInfo.AssigningContactType
        };
        assigningContactPoints.Add(newContactPoint);
      });

      _dataContext.ContactPoint.AddRange(assigningContactPoints);
      await _dataContext.SaveChangesAsync();

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrganisationContactPoints}-{ciiOrganisationId}");

      return assigningContactPoints.Select(acp => acp.Id).ToList();
    }

    /// <summary>
    /// Create a contact point for organisation including only the virtual addresses and contact person
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task<int> CreateOrganisationContactAsync(string ciiOrganisationId, ContactRequestInfo contactInfo)
    {
      await _contactsHelper.ValidateContactsAsync(contactInfo);

      var organisation = await _dataContext.Organisation
        .Include(o => o.Party)
        .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactPointReason);

        #region Create new contact person with party and contact etails
        var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

        var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

        var person = new Person
        {
          FirstName = firstName,
          LastName = lastName,
          OrganisationId = organisation.Id
        };

        var party = new Party
        {
          PartyTypeId = personPartyTypeId,
          Person = person,
          ContactPoints = new List<ContactPoint>()
        };

        var personContactPoint = new ContactPoint
        {
          ContactPointReasonId = contactPointReasonId,
          PartyTypeId = personPartyTypeId,
          ContactDetail = new ContactDetail
          {
            EffectiveFrom = DateTime.UtcNow
          }
        };
        await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, personContactPoint);
        party.ContactPoints.Add(personContactPoint);
        _dataContext.Party.Add(party);
        #endregion

        #region Add new contact point with created contact details
        // Link the created contact details to organisation by adding a contact point with a party type of EX/IN - ORGANISATION
        var orgContactPoint = new ContactPoint
        {
          PartyId = organisation.Party.Id,
          PartyTypeId = organisation.Party.PartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          ContactDetail = personContactPoint.ContactDetail
        };
        _dataContext.ContactPoint.Add(orgContactPoint);
        #endregion

        await _dataContext.SaveChangesAsync();

        var contactIds = personContactPoint.ContactDetail.VirtualAddresses != null ? personContactPoint.ContactDetail.VirtualAddresses.Select(va => va.Id).ToList()
          : new List<int>();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgContactCreate, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, OrgContactPointId:{orgContactPoint.Id}, ContactDetailId:{personContactPoint.ContactDetail.Id}" +
          $", OriginalContactPointId:{personContactPoint.Id}, ContactIds:{string.Join(",", contactIds)}" +
          $", RequestContactTypes:{string.Join(",", contactInfo.Contacts.Select(c => c.ContactType))}" +
          $", RequestContactPointReason:{contactInfo.ContactPointReason}");

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrganisationContactPoints}-{ciiOrganisationId}");

        // Notify Adapter
        await _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.OrgContact, OperationType.Create, ciiOrganisationId, contactIds);

        return orgContactPoint.Id;
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Delete a contact from organisation. Only do a soft deletion.
    /// Contact person also get deleted (soft deleted)
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task DeleteOrganisationContactAsync(string ciiOrganisationId, int contactId)
    {
      var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      // Taking the contact point and its person party with contactId and not a site and site contact and not a registered(Physical address (ContactPointReason is OTHER)) contact point
      var deletingContactPoint = await _dataContext.ContactPoint.Where(c => c.Id == contactId && !c.IsDeleted && c.AssignedContactType == AssignedContactType.None &&
        !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
        c.Party.Organisation.CiiOrganisationId == ciiOrganisationId && !c.Party.Organisation.IsDeleted)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses).ThenInclude(va => va.VirtualAddressType)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
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

        var deletingPersonContactPoint = deletingContactPoint.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);
        deletingPersonContactPoint.Party.Person.IsDeleted = true;

        await _dataContext.SaveChangesAsync();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgContactDelete, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, OrgContactPointId:{contactId}, ContactDetailId:{deletingContactPoint.ContactDetailId}" +
          $", DeletedContactIds:{string.Join(",", deletingVirtualAddresses.Select(va => va.Id))}, DeletedContactTypes:{string.Join(",", deletingVirtualAddresses.Select(va => va.VirtualAddressType.Name))}" +
          $", DeletingContactPointReason:{deletingContactPoint.ContactPointReason.Name}");

        //Invalidate redis
        var invalidatingCacheKeys = new List<string>();
        invalidatingCacheKeys.AddRange(deletingContactPoint.ContactDetail.VirtualAddresses.Select(va => $"{CacheKeyConstant.Contact}-{va.Id}").ToList());
        invalidatingCacheKeys.Add($"{CacheKeyConstant.OrganisationContactPoints}-{ciiOrganisationId}");
        await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());

        // Notify Adapter
        var contactIds = deletingContactPoint.ContactDetail.VirtualAddresses.Select(va => va.Id).ToList();
        await _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.OrgContact, OperationType.Delete, ciiOrganisationId, contactIds);
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Get list of virtual contacts for a contact point as contacts including the name
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task<OrganisationContactInfo> GetOrganisationContactAsync(string ciiOrganisationId, int contactId)
    {
      var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(p => p.PartyTypeName == PartyTypeName.NonUser)).Id;

      // Taking the contact point and its person party with contactId and not a site and site contact and not a registered(Physical address (ContactPointReason is OTHER)) contact point
      var contact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Id == contactId &&
        !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
        !c.Party.Organisation.IsDeleted && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (contact != null)
      {
        var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

        var personContactPoint = contact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);

        var contactInfo = new OrganisationContactInfo
        {
          ContactPointId = contact.Id,
          AssignedContactType = contact.AssignedContactType,
          OriginalContactPointId = contact.OriginalContactPointId,
          Detail = new OrganisationDetailInfo
          {
            OrganisationId = ciiOrganisationId,
          },
          ContactPointReason = await _contactsHelper.GetContactPointReasonNameAsync(personContactPoint.ContactPointReasonId),
          Contacts = new List<ContactResponseDetail>()
        };

        _contactsHelper.AssignVirtualContactsToContactResponse(personContactPoint, virtualContactTypes, contactInfo);

        return contactInfo;
      }

      throw new ResourceNotFoundException();
    }

    /// <summary>
    /// Get list of contact points with only virtual contacts (not include addresses since its in sites section)
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <returns></returns>
    public async Task<OrganisationContactInfoList> GetOrganisationContactsListAsync(string ciiOrganisationId, string contactType = null,
      ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All)
    {
      if (!await _dataContext.Organisation.AnyAsync(o => o.CiiOrganisationId == ciiOrganisationId))
      {
        throw new ResourceNotFoundException();
      }

      var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      List<ContactResponseInfo> contactInfos = new List<ContactResponseInfo>();

      // Taking the contact points and there person party which are and not sites and site contacts and not a registered(Physical address (ContactPointReason is OTHER)) contact point
      var contacts = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId &&
          !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
          (contactType == null || c.ContactPointReason.Name == contactType) &&
          (contactAssignedStatus == ContactAssignedStatus.All ||
            (contactAssignedStatus == ContactAssignedStatus.Original && c.AssignedContactType == AssignedContactType.None) ||
            (contactAssignedStatus == ContactAssignedStatus.Assigned && c.AssignedContactType != AssignedContactType.None)))
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
        .ToListAsync();

      var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

      foreach (var contact in contacts)
      {
        var personContactPoint = contact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);

        var contactInfo = new ContactResponseInfo
        {
          ContactPointId = contact.Id,
          AssignedContactType = contact.AssignedContactType,
          OriginalContactPointId = contact.OriginalContactPointId,
          ContactPointReason = await _contactsHelper.GetContactPointReasonNameAsync(personContactPoint.ContactPointReasonId),
          Contacts = new List<ContactResponseDetail>()
        };

        _contactsHelper.AssignVirtualContactsToContactResponse(personContactPoint, virtualContactTypes, contactInfo);

        contactInfos.Add(contactInfo);
      }

      return new OrganisationContactInfoList
      {
        Detail = new OrganisationDetailInfo
        {
          OrganisationId = ciiOrganisationId,
        },
        ContactPoints = contactInfos
      };
    }

    /// <summary>
    /// Unassign organisation contacts
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="unassigningContactPointIds"> Ids returned after assignment </param>
    /// <returns></returns>
    public async Task UnassignOrganisationContactsAsync(string ciiOrganisationId, List<int> unassigningContactPointIds)
    {
      if (unassigningContactPointIds == null || !unassigningContactPointIds.Any())
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUnassigningContactIds);
      }

      var organisation = await _dataContext.Organisation
        .Where(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId)
        .Include(o => o.Party).ThenInclude(p => p.ContactPoints.Where(cp => !cp.IsDeleted && !cp.IsSite && cp.AssignedContactType != AssignedContactType.None))
        .FirstOrDefaultAsync(); // TODO Check Where condition not working inside ThenInclude

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var invalidContactPointIds = unassigningContactPointIds
        .Where(unassigningId => !organisation.Party.ContactPoints.Any(cp => cp.Id == unassigningId && !cp.IsDeleted && !cp.IsSite && cp.AssignedContactType != AssignedContactType.None)).ToList();

      if (invalidContactPointIds.Any())
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUnassigningContactIds);
      }

      unassigningContactPointIds.ForEach((unassigningId) =>
      {
        var unassigningContact = organisation.Party.ContactPoints.First(cp => cp.Id == unassigningId);
        unassigningContact.IsDeleted = true;
      });

      await _dataContext.SaveChangesAsync();

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrganisationContactPoints}-{ciiOrganisationId}");
    }

    /// <summary>
    /// Update organisation contact
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task UpdateOrganisationContactAsync(string ciiOrganisationId, int contactId, ContactRequestInfo contactInfo)
    {
      await _contactsHelper.ValidateContactsAsync(contactInfo);

      var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      // Taking the contact point and its person party with contactId and not a site and site contact and not a registered(Physical address (ContactPointReason is OTHER)) contact point
      var updatingContact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Id == contactId &&
        !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
        c.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (updatingContact != null)
      {
        var previousVirtualContacts = updatingContact.ContactDetail.VirtualAddresses.Select(va => new KeyValuePair<int, string>(va.Id, va.VirtualAddressValue)).ToList();
        var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

        var updatingPersonContactPoint = updatingContact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);

        updatingPersonContactPoint.Party.Person.FirstName = firstName;
        updatingPersonContactPoint.Party.Person.LastName = lastName;

        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactPointReason);

        updatingPersonContactPoint.ContactPointReasonId = contactPointReasonId;

        await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, updatingPersonContactPoint);

        await _dataContext.SaveChangesAsync();

        var updatedVirtualContacts = updatingContact.ContactDetail.VirtualAddresses.Select(va => new KeyValuePair<int, string>(va.Id, va.VirtualAddressValue)).ToList();

        var createdContactIds = updatedVirtualContacts.Where(uc => !previousVirtualContacts.Any(pc => pc.Key == uc.Key)).Select(uc => uc.Key).ToList();
        var deletedContactIds = previousVirtualContacts.Where(pc => !updatedVirtualContacts.Any(uc => uc.Key == pc.Key)).Select(pc => pc.Key).ToList();
        var updatedContactIds = previousVirtualContacts.Where(pc => updatedVirtualContacts.Any(uc => uc.Key == pc.Key)).Select(pc => pc.Key).ToList();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgContactUpdate, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, OrgContactPointId:{contactId}, ContactDetailId:{updatingContact.ContactDetailId}" +
          $", AddedContactIds:{string.Join(",", createdContactIds)}, DeletedContactIds:{string.Join(",", deletedContactIds)}, UpdatedContactIds:{string.Join(",", updatedContactIds)}" +
          $", RequestContactTypes:{string.Join(",", contactInfo.Contacts.Select(c => c.ContactType))}" +
          $", RequestContactPointReason:{contactInfo.ContactPointReason}");

        //Invalidate redis
        var invalidatingCacheKeys = new List<string>();
        deletedContactIds.ForEach((id) => invalidatingCacheKeys.Add($"{CacheKeyConstant.Contact}-{id}"));
        updatedContactIds.ForEach((id) => invalidatingCacheKeys.Add($"{CacheKeyConstant.Contact}-{id}"));
        invalidatingCacheKeys.Add($"{CacheKeyConstant.OrganisationContactPoints}-{ciiOrganisationId}");
        await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());

        // Notify Adapter
        var createdContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.OrgContact, OperationType.Create, ciiOrganisationId, createdContactIds);
        var deletedContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.OrgContact, OperationType.Delete, ciiOrganisationId, deletedContactIds);
        var updatedContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.OrgContact, OperationType.Update, ciiOrganisationId, updatedContactIds);

        await Task.WhenAll(createdContactNotifyTask, deletedContactNotifyTask, updatedContactNotifyTask);
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }
  }
}
