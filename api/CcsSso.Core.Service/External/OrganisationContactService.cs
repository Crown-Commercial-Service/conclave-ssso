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
  public class OrganisationContactService : IOrganisationContactService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    public OrganisationContactService(IDataContext dataContext, IContactsHelperService contactsHelper)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
    }

    /// <summary>
    /// Create a contact point for organisation including only the virtual addresses and contact person
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task<int> CreateOrganisationContactAsync(string ciiOrganisationId, ContactInfo contactInfo)
    {
      _contactsHelper.ValidateContacts(contactInfo);

      var organisation = await _dataContext.Organisation
        .Include(o => o.Party)
        .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactReason);

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
        var userContactPoint = new ContactPoint
        {
          PartyId = organisation.Party.Id,
          PartyTypeId = organisation.Party.PartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          ContactDetail = personContactPoint.ContactDetail
        };
        _dataContext.ContactPoint.Add(userContactPoint);
        #endregion

        await _dataContext.SaveChangesAsync();

        return userContactPoint.Id;
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
      var deletingContact = await _dataContext.ContactPoint.Where(c => c.Id == contactId && !c.IsDeleted &&
        !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
        c.Party.Organisation.CiiOrganisationId == ciiOrganisationId && !c.Party.Organisation.IsDeleted)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp=> cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (deletingContact != null)
      {
        deletingContact.IsDeleted = true;
        deletingContact.ContactDetail.IsDeleted = true;

        deletingContact.ContactDetail.VirtualAddresses.ForEach((virtualAddress) =>
        {
          virtualAddress.IsDeleted = true;
        });

        var deletingPersonContactPoint = deletingContact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);
        deletingPersonContactPoint.Party.Person.IsDeleted = true;

        await _dataContext.SaveChangesAsync();
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

      var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      // Taking the contact point and its person party with contactId and not a site and site contact and not a registered(Physical address (ContactPointReason is OTHER)) contact point
      var contact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Id == contactId &&
        !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
        !c.Party.Organisation.IsDeleted && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (contact != null)
      {
        var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

        var contactInfo = new OrganisationContactInfo
        {
          ContactId = contact.Id,
          OrganisationId = ciiOrganisationId,
          ContactReason = contact.ContactPointReason.Name
        };

        var personContactPoint = contact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);

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
    public async Task<OrganisationContactInfoList> GetOrganisationContactsListAsync(string ciiOrganisationId, string contactType = null)
    {
      var personPartyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      List<ContactResponseInfo> contactInfos = new List<ContactResponseInfo>();

      // Taking the contact points and there person party which are and not sites and site contacts and not a registered(Physical address (ContactPointReason is OTHER)) contact point
      var contacts = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId &&
          !c.IsSite && c.ContactPointReason.Name != ContactReasonType.Site && c.ContactPointReason.Name != ContactReasonType.Other &&
          (contactType == null || c.ContactPointReason.Name == contactType))
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
        .ToListAsync();

      if (!contacts.Any() && !await _dataContext.Organisation.AnyAsync(o => o.CiiOrganisationId == ciiOrganisationId))
      {
        throw new ResourceNotFoundException();
      }

      var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

      foreach (var contact in contacts)
      {
        var contactInfo = new ContactResponseInfo
        {
          ContactId = contact.Id,
          ContactReason = contact.ContactPointReason.Name
        };

        var personContactPoint = contact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);

        _contactsHelper.AssignVirtualContactsToContactResponse(personContactPoint, virtualContactTypes, contactInfo);

        contactInfos.Add(contactInfo);
      }

      return new OrganisationContactInfoList
      {
        OrganisationId = ciiOrganisationId,
        ContactsList = contactInfos
      };
    }

    /// <summary>
    /// Update organisation contact
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="contactId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    public async Task UpdateOrganisationContactAsync(string ciiOrganisationId, int contactId, ContactInfo contactInfo)
    {
      _contactsHelper.ValidateContacts(contactInfo);

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
        var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

        var updatingPersonContactPoint = updatingContact.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);

        updatingPersonContactPoint.Party.Person.FirstName = firstName;
        updatingPersonContactPoint.Party.Person.LastName = lastName;

        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactReason);

        updatingContact.ContactPointReasonId = contactPointReasonId;

        await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, updatingPersonContactPoint);

        await _dataContext.SaveChangesAsync();
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }
  }
}
