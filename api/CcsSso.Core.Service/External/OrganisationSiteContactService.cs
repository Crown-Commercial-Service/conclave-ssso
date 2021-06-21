using CcsSso.Core.DbModel.Entity;
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

namespace CcsSso.Core.Service.External
{
  public class OrganisationSiteContactService : IOrganisationSiteContactService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    private IAdaptorNotificationService _adaptorNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;

    public OrganisationSiteContactService(IDataContext dataContext, IContactsHelperService contactsHelper,
      IAdaptorNotificationService adaptorNotificationService, IWrapperCacheService wrapperCacheService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _adaptorNotificationService = adaptorNotificationService;
      _wrapperCacheService = wrapperCacheService;
    }

    /// <summary>
    /// Create the Site contact. This create a person contact point for the organisation of the site and then assign that contact point to site using site contact entity
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task<int> CreateOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, ContactRequestInfo contactInfo)
    {
      await _contactsHelper.ValidateContactsAsync(contactInfo);

      var organisationSiteContactPoint = await _dataContext.ContactPoint
        .Include(cp => cp.Party).ThenInclude(p => p.Organisation)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .FirstOrDefaultAsync(cp => !cp.IsDeleted && cp.IsSite && cp.Id == siteId && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId);

      if (organisationSiteContactPoint != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactPointReason);

        #region Create new contact point for person with party
        var personPartyTypeId = (await _dataContext.PartyType.Where(pt => pt.PartyTypeName == PartyTypeName.NonUser).FirstOrDefaultAsync()).Id;

        // Create the contact point with contact details including the person details, having the party type of NON_USER
        var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

        var person = new Person
        {
          FirstName = firstName,
          LastName = lastName,
          OrganisationId = organisationSiteContactPoint.Party.Organisation.Id
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

        // Create the site contact with, person contact contactPointId and site id(site contactPointId)
        var siteContact = new SiteContact
        {
          ContactPointId = organisationSiteContactPoint.Id,
          ContactId = contactPoint.Id
        };

        _dataContext.SiteContact.Add(siteContact);
        await _dataContext.SaveChangesAsync();

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.SiteContactPoints}-{ciiOrganisationId}-{siteId}");

        // Notify Adapter
        var contactIds = contactPoint.ContactDetail.VirtualAddresses.Select(va => va.Id).ToList();
        await _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.SiteContact, OperationType.Create, ciiOrganisationId, contactIds);

        return siteContact.Id;
      }

      throw new ResourceNotFoundException();
    }

    /// <summary>
    /// Soft delete the site contact.
    /// This only soft delete the site contact record.
    /// Does not delete the contact person details including virtual contacts since it can be deleted using organisation contact deletion.
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task DeleteOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId)
    {
      // Site contact
      var deletingSiteContact = await _dataContext.SiteContact
        .FirstOrDefaultAsync(c => c.Id == contactId && !c.IsDeleted && c.ContactPointId == siteId && c.ContactPoint.Party.Organisation.CiiOrganisationId == ciiOrganisationId);

      // Check whether ther is a site, organisation and contact with given ids.
      if (deletingSiteContact == null)
      {
        throw new ResourceNotFoundException();
      }

      // Get the actual contact point including the site contact person information
      var deletingSiteContactPoint = await _dataContext.ContactPoint
        .Where(cp => !cp.IsDeleted && !cp.IsSite && cp.Id == deletingSiteContact.ContactId)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(cp => cp.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      deletingSiteContact.IsDeleted = true;
      deletingSiteContactPoint.IsDeleted = true;
      deletingSiteContactPoint.Party.Person.IsDeleted = true;

      if (deletingSiteContactPoint.ContactDetail != null)
      {
        deletingSiteContactPoint.ContactDetail.IsDeleted = true;
        deletingSiteContactPoint.ContactDetail.VirtualAddresses.ForEach((va) => va.IsDeleted = true);
      }

      await _dataContext.SaveChangesAsync();

      if (deletingSiteContactPoint.ContactDetail != null)
      {
        //Invalidate redis
        var invalidatingCacheKeys = new List<string>();
        invalidatingCacheKeys.AddRange(deletingSiteContactPoint.ContactDetail.VirtualAddresses.Select(va => $"{CacheKeyConstant.Contact}-{va.Id}").ToList());
        invalidatingCacheKeys.Add($"{CacheKeyConstant.SiteContactPoints}-{ciiOrganisationId}-{siteId}");
        await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());

        // Notify Adapter
        var contactIds = deletingSiteContactPoint.ContactDetail.VirtualAddresses.Select(va => va.Id).ToList();
        await _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.SiteContact, OperationType.Delete, ciiOrganisationId, contactIds); 
      }
    }

    /// <summary>
    /// Get the organisation site contact detail by contact id.
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task<OrganisationSiteContactInfo> GetOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId)
    {
      // Site contact
      var siteContact = await _dataContext.SiteContact
        .FirstOrDefaultAsync(c => c.Id == contactId && !c.IsDeleted && c.ContactPointId == siteId && c.ContactPoint.Party.Organisation.CiiOrganisationId == ciiOrganisationId);

      // Check whether there is a site, organisation and contact with given ids.
      if (siteContact == null)
      {
        throw new ResourceNotFoundException();
      }

      // Get the actual contact point which has the site contact person information
      var siteContactDetailContactPoint = await _dataContext.ContactPoint
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail.VirtualAddresses)
        .FirstOrDefaultAsync(cp => !cp.IsDeleted && !cp.IsSite && cp.Id == siteContact.ContactId);

      // Check for null since this doesn't have a foriegn key
      if (siteContactDetailContactPoint != null)
      {
        var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

        var organisationSiteContactInfo = new OrganisationSiteContactInfo
        {
          ContactPointId = contactId,
          Detail = new SiteDetailInfo
          {
            OrganisationId = ciiOrganisationId,
            SiteId = siteId,
          },
          ContactPointReason = siteContactDetailContactPoint.ContactPointReason.Name,
          Contacts = new List<ContactResponseDetail>()
        };

        _contactsHelper.AssignVirtualContactsToContactResponse(siteContactDetailContactPoint, virtualContactTypes, organisationSiteContactInfo);

        return organisationSiteContactInfo;
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    public async Task<OrganisationSiteContactInfoList> GetOrganisationSiteContactsListAsync(string ciiOrganisationId, int siteId, string contactType = null)
    {
      // The actual site which is also a contact point
      var siteContactPoint = await _dataContext.ContactPoint
        .Include(c => c.SiteContacts)
        .FirstOrDefaultAsync(c => !c.IsDeleted && c.Id == siteId && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId);

      // Check whether there is a site, organisation with given ids.
      if (siteContactPoint == null)
      {
        throw new ResourceNotFoundException();
      }

      List<ContactResponseInfo> siteContactList = new List<ContactResponseInfo>();

      var sitePersonContactPointIds = siteContactPoint.SiteContacts.Where(s => !s.IsDeleted).Select(s => s.ContactId).ToList();

      // Get the original contact person details for all the contacts in the site
      // Have to do this way since there is no navigation property associated with siteContact contactId and contactPoint Id
      var sitePersonContacts = await _dataContext.ContactPoint
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail.VirtualAddresses)
        .Where(cp => sitePersonContactPointIds.Contains(cp.Id) && !cp.IsDeleted &&
          (contactType == null || cp.ContactPointReason.Name == contactType))
        .ToListAsync();

      var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

      foreach (var sitePersonContact in sitePersonContacts)
      {
        var contactId = siteContactPoint.SiteContacts.FirstOrDefault(sc => sc.ContactId == sitePersonContact.Id).Id;
        var siteContactResponseInfo = new ContactResponseInfo
        {
          ContactPointId = contactId,
          ContactPointReason = sitePersonContact.ContactPointReason.Name,
          Contacts = new List<ContactResponseDetail>()
        };

        _contactsHelper.AssignVirtualContactsToContactResponse(sitePersonContact, virtualContactTypes, siteContactResponseInfo);

        siteContactList.Add(siteContactResponseInfo);
      }

      return new OrganisationSiteContactInfoList
      {
        Detail = new SiteDetailInfo
        {
          OrganisationId = ciiOrganisationId,
          SiteId = siteId
        },
        ContactPoints = siteContactList
      };
    }

    public async Task UpdateOrganisationSiteContactAsync(string ciiOrganisationId, int siteId, int contactId, ContactRequestInfo contactInfo)
    {
      await _contactsHelper.ValidateContactsAsync(contactInfo);

      // Site contact
      var updatingSiteContact = await _dataContext.SiteContact
        .FirstOrDefaultAsync(c => c.Id == contactId && !c.IsDeleted && c.ContactPointId == siteId && c.ContactPoint.Party.Organisation.CiiOrganisationId == ciiOrganisationId);

      // Check whether ther is a site, organisation and contact with given ids.
      if (updatingSiteContact == null)
      {
        throw new ResourceNotFoundException();
      }

      // Get the actual contact point including the site contact person information
      var updatingContact = await _dataContext.ContactPoint
        .Where(cp => !cp.IsDeleted && !cp.IsSite && cp.Id == updatingSiteContact.ContactId)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(cp => cp.Party).ThenInclude(p => p.Person)
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

        //Invalidate redis
        var invalidatingCacheKeys = new List<string>();
        deletedContactIds.ForEach((id) => invalidatingCacheKeys.Add($"{CacheKeyConstant.Contact}-{id}"));
        updatedContactIds.ForEach((id) => invalidatingCacheKeys.Add($"{CacheKeyConstant.Contact}-{id}"));
        invalidatingCacheKeys.Add($"{CacheKeyConstant.SiteContactPoints}-{ciiOrganisationId}-{siteId}");
        await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());

        // Notify Adapter
        var createdContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.SiteContact, OperationType.Create, ciiOrganisationId, createdContactIds);
        var deletedContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.SiteContact, OperationType.Delete, ciiOrganisationId, deletedContactIds);
        var updatedContactNotifyTask = _adaptorNotificationService.NotifyContactPointChangesAsync(ConclaveEntityNames.SiteContact, OperationType.Update, ciiOrganisationId, updatedContactIds);

        await Task.WhenAll(createdContactNotifyTask, deletedContactNotifyTask, updatedContactNotifyTask);
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }
  }
}
