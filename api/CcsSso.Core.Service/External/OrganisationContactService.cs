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

      var organisation = await _dataContext.Organisation.FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(contactInfo.ContactReason);

        #region Create new contact person with party
        var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

        var (firstName, lastName) = _contactsHelper.GetContactPersonNameTuple(contactInfo);

        var person = new Person
        {
          FirstName = firstName,
          LastName = lastName,
          OrganisationId = organisation.Id
        };

        var party = new Party
        {
          PartyTypeId = partyTypeId,
          Person = person,
          ContactPoints = new List<ContactPoint>()
        };
        #endregion

        var contactPoint = new ContactPoint
        {
          ContactPointReasonId = contactPointReasonId,
          PartyTypeId = partyTypeId,
          ContactDetail = new ContactDetail
          {
            EffectiveFrom = DateTime.UtcNow
          }
        };
        await _contactsHelper.AssignVirtualContactsToContactPointAsync(contactInfo, contactPoint);
        party.ContactPoints.Add(contactPoint);

        _dataContext.Party.Add(party);

        await _dataContext.SaveChangesAsync();

        return contactPoint.Id;
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
      var deletingContact = await _dataContext.ContactPoint.Where(c => c.Id == contactId && !c.IsDeleted
        && c.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId && !c.Party.Person.Organisation.IsDeleted)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (deletingContact != null)
      {
        deletingContact.IsDeleted = true;
        deletingContact.ContactDetail.IsDeleted = true;

        deletingContact.ContactDetail.VirtualAddresses.ForEach((virtualAddress) =>
        {
          virtualAddress.IsDeleted = true;
        });

        if (deletingContact.Party.Person != null)
        {
          deletingContact.Party.Person.IsDeleted = true;
        }

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
      var contact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Id == contactId && c.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId)
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail.VirtualAddresses)
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

        _contactsHelper.AssignVirtualContactsToContactResponse(contact, virtualContactTypes, contactInfo);

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
      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      List<ContactResponseInfo> contactInfos = new List<ContactResponseInfo>();

      var contacts = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.PartyTypeId == partyTypeId &&
          c.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId &&
          (contactType == null || c.ContactPointReason.Name == contactType))
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .Include(c => c.ContactPointReason)
        .Include(c => c.ContactDetail.VirtualAddresses)
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

        _contactsHelper.AssignVirtualContactsToContactResponse(contact, virtualContactTypes, contactInfo);

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

      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;

      var updatingContact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.PartyTypeId == partyTypeId &&
          c.Id == contactId && c.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId)
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
  }
}
