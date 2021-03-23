using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Service
{
  public class ContactService : IContactService
  {

    private readonly IDataContext _dataContext;
    public ContactService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    /// <summary>
    /// Create the contact details. Contact Type is required.
    /// This method is the wrapper to create contacts of all the contact types.
    /// For organisation person contact, organisation id is required.
    /// For organisation contact, organisation id is required
    /// </summary>
    /// <param name="contactDetailModel"></param>
    /// <returns></returns>
    public async Task<int> CreateAsync(ContactDetailDto contactDetailModel)
    {
      var conatactId = contactDetailModel.ContactType switch
      {
        ContactType.Organisation => await CreateOrganisationContactAsync(contactDetailModel),
        ContactType.OrganisationPerson => await CreateOrganisationPersonContactAsync(contactDetailModel),
        ContactType.User => throw new NotImplementedException(),
        _ => throw new CcsSsoException(Contstant.InvalidContactType),
      };
      return conatactId;
    }

    /// <summary>
    /// Delete contact by contact id
    /// </summary>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task DeleteAsync(int contactId)
    {
      var deletingContact = await _dataContext.ContactPoint.Where(c => c.Id == contactId)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (deletingContact != null)
      {
        deletingContact.IsDeleted = true;
        if (deletingContact.ContactDetail != null)
        {
          deletingContact.ContactDetail.IsDeleted = true;
          if (deletingContact.ContactDetail.PhysicalAddress != null)
          {
            deletingContact.ContactDetail.PhysicalAddress.IsDeleted = true;
          }
          if (deletingContact.ContactDetail.VirtualAddresses != null)
          {
            deletingContact.ContactDetail.VirtualAddresses.ForEach((virtualAddress) =>
            {
              virtualAddress.IsDeleted = true;
            });
          }
        }
        if (deletingContact.Party.Person != null)
        {
          deletingContact.Party.Person.IsDeleted = true;
        }

        await _dataContext.SaveChangesAsync();
      }
    }

    /// <summary>
    /// Get contact details
    /// Filters :- OrganisationId, UserId
    /// </summary>
    /// <param name="contactRequestFilter"></param>
    /// <returns></returns>
    public async Task<List<ContactDetailDto>> GetAsync(ContactRequestFilter contactRequestFilter)
    {
      List<ContactDetailDto> contactDetailDtos = new List<ContactDetailDto>();

      var contacts = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted &&
        (c.Party.Organisation.Id == contactRequestFilter.OrganisationId
          || c.Party.Person.OrganisationId == contactRequestFilter.OrganisationId))
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Include(c => c.Party).ThenInclude(p => p.Organisation)
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .ToListAsync();

      var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

      foreach (var contact in contacts)
      {
        var contactDto = new ContactDetailDto
        {
          ContactId = contact.Id,
          PartyId = contact.PartyId,
          Address = contact.ContactDetail.PhysicalAddress != null ? new Address
          {
            StreetAddress = contact.ContactDetail.PhysicalAddress.StreetAddress,
            Locality = contact.ContactDetail.PhysicalAddress.Locality,
            Region = contact.ContactDetail.PhysicalAddress.Region,
            CountryCode = contact.ContactDetail.PhysicalAddress.CountryCode,
            PostalCode = contact.ContactDetail.PhysicalAddress.PostalCode,
            Uprn = contact.ContactDetail.PhysicalAddress.Uprn
          } : null,
        };

        if (contact.ContactDetail.VirtualAddresses != null && contact.ContactDetail.VirtualAddresses.Any())
        {
          contactDto.Email = contact.ContactDetail.VirtualAddresses.FirstOrDefault(va =>
          va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct => vct.Name == VirtualContactTypeName.Email)?.Id)?.VirtualAddressValue;
          contactDto.PhoneNumber = contact.ContactDetail.VirtualAddresses.FirstOrDefault(va =>
          va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct => vct.Name == VirtualContactTypeName.Phone)?.Id)?.VirtualAddressValue;
          contactDto.Fax = contact.ContactDetail.VirtualAddresses.FirstOrDefault(va =>
          va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct => vct.Name == VirtualContactTypeName.Fax)?.Id)?.VirtualAddressValue;
          contactDto.WebUrl = contact.ContactDetail.VirtualAddresses.FirstOrDefault(va =>
          va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct => vct.Name == VirtualContactTypeName.Url)?.Id)?.VirtualAddressValue;
        }

        // If an organisation is associated with contact then include the organisation name
        if (contact.Party.Organisation != null)
        {
          contactDto.ContactType = ContactType.Organisation;
          contactDto.Name = contact.Party.Organisation.OrganisationUri;
        }
        // If person is associated with contact then include the person name
        else if (contact.Party.Person != null)
        {
          contactDto.ContactType = ContactType.OrganisationPerson;
          contactDto.Name = $"{contact.Party.Person.FirstName} {contact.Party.Person.LastName}";
        }

        contactDetailDtos.Add(contactDto);
      }

      return contactDetailDtos;
    }

    public async Task<ContactDetailDto> GetAsync(int contactId)
    {
      var contact = await _dataContext.ContactPoint
        .Where(c => !c.IsDeleted && c.Id == contactId)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .Include(c => c.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Include(c => c.Party).ThenInclude(p => p.Organisation)
        .Include(c => c.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      var virtualContactTypes = await _dataContext.VirtualAddressType.ToListAsync();

      if (contact != null)
      {
        var contactDto = new ContactDetailDto
        {
          ContactId = contact.Id,
          PartyId = contact.PartyId,
          Address = contact.ContactDetail.PhysicalAddress != null ? new Address
          {
            StreetAddress = contact.ContactDetail.PhysicalAddress.StreetAddress,
            Locality = contact.ContactDetail.PhysicalAddress.Locality,
            Region = contact.ContactDetail.PhysicalAddress.Region,
            CountryCode = contact.ContactDetail.PhysicalAddress.CountryCode,
            PostalCode = contact.ContactDetail.PhysicalAddress.PostalCode,
            Uprn = contact.ContactDetail.PhysicalAddress.Uprn
          } : null,
          Email = contact.ContactDetail.VirtualAddresses.FirstOrDefault(va =>
          va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct => vct.Name == VirtualContactTypeName.Email)?.Id)?.VirtualAddressValue,
          PhoneNumber = contact.ContactDetail.VirtualAddresses.FirstOrDefault(va =>
          va.VirtualAddressTypeId == virtualContactTypes.FirstOrDefault(vct => vct.Name == VirtualContactTypeName.Phone)?.Id)?.VirtualAddressValue
        };

        // If an organisation is associated with contact then include the organisation name
        if (contact.Party.Organisation != null)
        {
          contactDto.ContactType = ContactType.Organisation;
          contactDto.Name = contact.Party.Organisation.OrganisationUri;
        }
        // If person is associated with contact then include the person name
        else if (contact.Party.Person != null)
        {
          contactDto.ContactType = ContactType.OrganisationPerson;
          contactDto.Name = $"{contact.Party.Person.FirstName} {contact.Party.Person.LastName}";
        }

        return contactDto;
      }

      return null;
    }

    /// <summary>
    /// Update the contact details. Contact Type is required.
    /// This method is the wrapper to update contacts of all the contact types.
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="contactDetailDto"></param>
    /// <returns></returns>
    public async Task<int> UpdateAsync(int contactId, ContactDetailDto contactDetailDto)
    {
      return contactDetailDto.ContactType switch
      {
        ContactType.Organisation => throw new NotImplementedException(),
        ContactType.OrganisationPerson => await UpdateOrganisationPersonContactAsync(contactId, contactDetailDto),
        ContactType.User => throw new NotImplementedException(),
        _ => throw new CcsSsoException(Contstant.InvalidContactType),
      };
    }


    #region Private methods

    /// <summary>
    /// Create the contact for organisation (when ContactType is Organisation).
    /// This method creates the physical address of the organisation.
    /// Organisation id is required
    /// </summary>
    /// <param name="contactDetailsDto"></param>
    /// <returns></returns>
    private async Task<int> CreateOrganisationContactAsync(ContactDetailDto contactDetailsDto)
    {
      ValidateOrganisationContactPersonDetail(contactDetailsDto);

      var contactPointReasonId = await GetContactPointReasonIdAsync(contactDetailsDto.ContactReason);

      var organisation = await _dataContext.Organisation
        .Include(o => o.Party)
        .FirstOrDefaultAsync(o => o.Id == contactDetailsDto.OrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var contactPoint = new ContactPoint
      {
        PartyId = organisation.PartyId,
        PartyTypeId = organisation.Party.PartyTypeId,
        ContactPointReasonId = contactPointReasonId,
        ContactDetail = new ContactDetail
        {
          EffectiveFrom = DateTime.UtcNow
        }
      };
      await UpdateContactPointInMemoryEntityAsync(contactDetailsDto, contactPoint);

      _dataContext.ContactPoint.Add(contactPoint);

      await _dataContext.SaveChangesAsync();

      return contactPoint.Id;
    }

    /// <summary>
    /// Create the contact for organisation people (when ContactType is OrganisationPerson).
    /// This method creates a person and the contact record.
    /// Organisation id is required.
    /// </summary>
    /// <param name="contactDetailsDto"></param>
    /// <returns></returns>
    private async Task<int> CreateOrganisationPersonContactAsync(ContactDetailDto contactDetailsDto)
    {
      ValidateOrganisationContactPersonDetail(contactDetailsDto);

      #region Create new contact person with party
      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;
      var contactPointReasonId = await GetContactPointReasonIdAsync(contactDetailsDto.ContactReason);

      var nameArray = contactDetailsDto.Name.Split(" ");

      var person = new Person
      {
        FirstName = nameArray[0],
        LastName = nameArray.Length == 2 ? nameArray[1] : string.Empty,
        OrganisationId = contactDetailsDto.OrganisationId
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
      await UpdateContactPointInMemoryEntityAsync(contactDetailsDto, contactPoint);
      party.ContactPoints.Add(contactPoint);

      _dataContext.Party.Add(party);

      await _dataContext.SaveChangesAsync();

      return contactPoint.Id;
    }
    /// <summary>
    /// Get the contact point reason id for the reason provided.
    /// If not return the id for OTHER reason option.
    /// </summary>
    /// <param name="reason"></param>
    /// <returns></returns>
    private async Task<int> GetContactPointReasonIdAsync(string reason)
    {
      var reasonString = string.IsNullOrEmpty(reason) ? ContactReasonType.Other : reason.ToUpper();

      var contactReasons = await _dataContext.ContactPointReason.ToListAsync();

      var contactPointReason = contactReasons.FirstOrDefault(r => r.Name == reasonString);

      if (contactPointReason != null)
      {
        return contactPointReason.Id;
      }
      return contactReasons.FirstOrDefault(r => r.Name == ContactReasonType.Other).Id;
    }

    /// <summary>
    /// Get the physical address entity
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private PhysicalAddress GetPhysicalAddress(Address address)
    {
      return new PhysicalAddress
      {
        StreetAddress = address.StreetAddress,
        Locality = address.Locality,
        Region = address.Region,
        PostalCode = address.PostalCode,
        CountryCode = address.CountryCode,
        Uprn = address.Uprn
      };
    }

    /// <summary>
    /// Get the list of virtual addresses from the contact detail model
    /// </summary>
    /// <param name="contactDetailModel"></param>
    /// <returns></returns>
    private async Task<List<VirtualAddress>> GetVirtualAddressesAsync(ContactDetailDto contactDetailModel)
    {
      var virtualAddressTypes = await _dataContext.VirtualAddressType.ToListAsync();

      List<VirtualAddress> virtualAddresses = new List<VirtualAddress>();

      if (!string.IsNullOrEmpty(contactDetailModel.Email))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Email).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactDetailModel.Email
        };
        virtualAddresses.Add(virtualAddress);
      }
      if (!string.IsNullOrEmpty(contactDetailModel.PhoneNumber))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Phone).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactDetailModel.PhoneNumber
        };
        virtualAddresses.Add(virtualAddress);
      }
      if (!string.IsNullOrEmpty(contactDetailModel.Fax))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Fax).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactDetailModel.Fax
        };
        virtualAddresses.Add(virtualAddress);
      }
      if (!string.IsNullOrEmpty(contactDetailModel.WebUrl))
      {
        var virtualAddressTypeId = virtualAddressTypes.FirstOrDefault(t => t.Name == VirtualContactTypeName.Url).Id;

        var virtualAddress = new VirtualAddress
        {
          VirtualAddressTypeId = virtualAddressTypeId,
          VirtualAddressValue = contactDetailModel.WebUrl
        };
        virtualAddresses.Add(virtualAddress);
      }
      return virtualAddresses;
    }

    /// <summary>
    /// Validate the contact details on both create and update.
    /// </summary>
    /// <param name="contactDetailModel"></param>
    private void ValidateOrganisationContactPersonDetail(ContactDetailDto contactDetailModel, bool isCreation = true)
    {
      if (!string.IsNullOrEmpty(contactDetailModel.Email) && !UtilitiesHelper.IsEmailValid(contactDetailModel.Email))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidEmail);
      }
    }

    /// <summary>
    /// Common method to create the ContactPoint in-memory entity to use in both contact creation and update methods.
    /// </summary>
    /// <param name="contactDetailsDto"></param>
    /// <param name="contactPoint"></param>
    /// <returns></returns>
    private async Task UpdateContactPointInMemoryEntityAsync(ContactDetailDto contactDetailsDto, ContactPoint contactPoint)
    {
      List<VirtualAddress> virtualAddresses = await GetVirtualAddressesAsync(contactDetailsDto);

      //if (!virtualAddresses.Any() && contactDetailsDto.Address == null)
      //{
      //  throw new CcsSsoException(ErrorConstant.ErrorInvalidContacts);
      //}

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

      if (contactDetailsDto.Address != null)
      {
        contactPoint.ContactDetail.PhysicalAddress = GetPhysicalAddress(contactDetailsDto.Address);
      }
    }

    /// <summary>
    /// Update the contact for organisation people (when ContactType is OrganisationPerson).
    /// This method updates the contact record and the person.
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="contactDetailsDto"></param>
    /// <returns></returns>
    private async Task<int> UpdateOrganisationPersonContactAsync(int contactId, ContactDetailDto contactDetailsDto)
    {
      ValidateOrganisationContactPersonDetail(contactDetailsDto, false);

      var updatingContact = await _dataContext.ContactPoint.Where(c => c.Id == contactId)
        .Include(cp => cp.ContactDetail)
        .Include(cd => cd.ContactDetail.PhysicalAddress)
        .Include(cd => cd.ContactDetail.VirtualAddresses)
        .Include(cp => cp.Party)
        .ThenInclude(p => p.Person)
        .FirstOrDefaultAsync();

      if (updatingContact != null)
      {
        await UpdateContactPointInMemoryEntityAsync(contactDetailsDto, updatingContact);

        var nameArray = contactDetailsDto.Name.Split(" ");

        updatingContact.Party.Person.FirstName = nameArray[0];
        updatingContact.Party.Person.LastName = nameArray.Length == 2 ? nameArray[1] : string.Empty;
        await _dataContext.SaveChangesAsync();
        return updatingContact.Id;
      }
      else
      {
        throw new CcsSsoException(ErrorConstant.EntityNotFound);
      }
    }
    #endregion
  }
}
