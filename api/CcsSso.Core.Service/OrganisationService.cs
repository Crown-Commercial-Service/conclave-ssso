using CcsSso.Core.DbModel.Entity;
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
  public class OrganisationService : IOrganisationService
  {

    private readonly IDataContext _dataContext;
    public OrganisationService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    /// <summary>
    /// Creates an organisation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<int> CreateAsync(OrganisationDto model)
    {
      var partyType = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == "EXTERNAL_ORGANISATION"));
      var party = new CcsSso.DbModel.Entity.Party
      {
        PartyTypeId = partyType.Id,
        CreatedPartyId = 0,
        LastUpdatedPartyId = 0,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();
      // TODO verify later with Lee this area since this is already done by the contact service and there are two api calls done by UI for this.
      //var contactDetail = new CcsSso.DbModel.Entity.ContactDetail
      //{
      //  EffectiveFrom = System.DateTime.UtcNow,
      //  CreatedPartyId = 0,
      //  LastUpdatedPartyId = 0,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.ContactDetail.Add(contactDetail);
      //await _dataContext.SaveChangesAsync();
      //var contactPoint = new CcsSso.DbModel.Entity.ContactPoint
      //{
      //  PartyId = party.Id,
      //  PartyTypeId = partyType.Id,
      //  ContactPointReasonId = 1,
      //  CreatedPartyId = 0,
      //  LastUpdatedPartyId = 0,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //  ContactDetailId = contactDetail.Id
      //};
      //_dataContext.ContactPoint.Add(contactPoint);
      //await _dataContext.SaveChangesAsync();
      //var physicalAddress = new CcsSso.DbModel.Entity.PhysicalAddress
      //{
      //  ContactDetailId = contactDetail.Id,
      //  StreetAddress = model.Address.StreetAddress,
      //  Locality = model.Address.Locality,
      //  Region = model.Address.Region,
      //  PostalCode = model.Address.PostalCode,
      //  CountryCode = model.Address.CountryCode,
      //  Uprn = model.Address.Uprn,
      //  CreatedPartyId = 0,
      //  LastUpdatedPartyId = 0,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.PhysicalAddress.Add(physicalAddress);
      //await _dataContext.SaveChangesAsync();
      //var virtualAddress1 = new VirtualAddress
      //{
      //  ContactDetailId = contactDetail.Id,
      //  VirtualAddressTypeId = (await _dataContext.VirtualAddressType.FirstOrDefaultAsync(t => t.Name == "EMAIL")).Id,
      //  VirtualAddressValue = model.ContactPoint.Email,
      //  CreatedPartyId = party.Id,
      //  LastUpdatedPartyId = party.Id,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //var virtualAddress2 = new VirtualAddress
      //{
      //  ContactDetailId = contactDetail.Id,
      //  VirtualAddressTypeId = (await _dataContext.VirtualAddressType.FirstOrDefaultAsync(t => t.Name == "FAX")).Id,
      //  VirtualAddressValue = model.ContactPoint.Fax,
      //  CreatedPartyId = party.Id,
      //  LastUpdatedPartyId = party.Id,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.VirtualAddress.Add(virtualAddress2);
      //var virtualAddress3 = new VirtualAddress
      //{
      //  ContactDetailId = contactDetail.Id,
      //  VirtualAddressTypeId = (await _dataContext.VirtualAddressType.FirstOrDefaultAsync(t => t.Name == "PHONE")).Id,
      //  VirtualAddressValue = model.ContactPoint.PhoneNumber,
      //  CreatedPartyId = party.Id,
      //  LastUpdatedPartyId = party.Id,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.VirtualAddress.Add(virtualAddress3);
      //var virtualAddress4 = new VirtualAddress
      //{
      //  ContactDetailId = contactDetail.Id,
      //  VirtualAddressTypeId = (await _dataContext.VirtualAddressType.FirstOrDefaultAsync(t => t.Name == "WEB_ADDRESS")).Id,
      //  VirtualAddressValue = model.ContactPoint.WebUrl,
      //  CreatedPartyId = party.Id,
      //  LastUpdatedPartyId = party.Id,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.VirtualAddress.Add(virtualAddress4);
      //await _dataContext.SaveChangesAsync();
      var org = new Organisation
      {
        CiiOrganisationId = model.CiiOrganisationId,
        OrganisationUri = model.OrganisationUri,
        LegalName = model.LegalName,
        RightToBuy = model.RightToBuy,
        PartyId = party.Id,
        CreatedPartyId = party.Id,
        LastUpdatedPartyId = party.Id,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.Organisation.Add(org);
      var group = new OrganisationUserGroup
      {
        Organisation = org,
        UserGroupName = "Organisation Administrator",
        UserGroupNameKey = "ORG_ADMINISTRATOR_GROUP",
      };
      _dataContext.OrganisationUserGroup.Add(group);
      var role = _dataContext.CcsAccessRole.FirstOrDefault(x => x.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");
      var roleMapping = new GroupAccess
      {
        OrganisationUserGroup = group,
        CcsAccessRole = role,
      };
      _dataContext.GroupAccess.Add(roleMapping);
      var group2 = new OrganisationUserGroup
      {
        Organisation = org,
        UserGroupName = "Default Organisation User",
        UserGroupNameKey = "ORG_DEFAULT_USER_GROUP",
      };
      _dataContext.OrganisationUserGroup.Add(group2);
      var role2 = _dataContext.CcsAccessRole.FirstOrDefault(x => x.CcsAccessRoleNameKey == "ORG_DEFAULT_USER");
      var roleMapping2 = new GroupAccess
      {
        OrganisationUserGroup = group2,
        CcsAccessRole = role2,
      };
      _dataContext.GroupAccess.Add(roleMapping2);
      await _dataContext.SaveChangesAsync();
      return org.Id;
    }

    /// <summary>
    /// Delete an organisation by its id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(int id)
    {
      var organisation = await _dataContext.Organisation
        .Where(x => x.Id == id)
        .SingleOrDefaultAsync();
      if (organisation != null)
      {
        organisation.IsDeleted = true;
        await _dataContext.SaveChangesAsync();
      }
    }

    /// <summary>
    /// Get an organisation by its id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<OrganisationDto> GetAsync(string id)
    {
      var organisation = await _dataContext.Organisation
        .Where(x => x.CiiOrganisationId == id && x.IsDeleted == false)
        .FirstOrDefaultAsync();
      if (organisation != null) {
        var dto = new OrganisationDto {
            OrganisationId = organisation.Id,
            CiiOrganisationId = organisation.CiiOrganisationId,
            OrganisationUri = organisation.OrganisationUri,
            RightToBuy = organisation.RightToBuy,
            PartyId = organisation.PartyId,
            LegalName = organisation.LegalName,
        };
        var contactPoint = await _dataContext.ContactPoint
          .Include(c => c.ContactDetail)
        .Where(x => x.PartyId == organisation.PartyId)
        .FirstOrDefaultAsync();
        if (contactPoint != null)
        {
          var contactDetail = contactPoint.ContactDetail;
          if (contactDetail != null) {
            var physicalAddress = await _dataContext.PhysicalAddress
            .Where(x => x.ContactDetailId == contactDetail.Id)
            .FirstOrDefaultAsync();
            if (physicalAddress != null)
            {
              dto.Address = new Address
              {
                StreetAddress = physicalAddress.StreetAddress,
                Region = physicalAddress.Region,
                PostalCode = physicalAddress.PostalCode,
                Locality = physicalAddress.Locality,
                CountryCode = physicalAddress.CountryCode,
                Uprn = physicalAddress.Uprn,
              };
            }
            dto.ContactPoint = new ContactDetailDto {
              Email = "",
              WebUrl = "",
              PhoneNumber = "",
              Fax = ""
            };
            var virtualAddress1 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 1)
            .FirstOrDefaultAsync();
            if (virtualAddress1 != null) {
              dto.ContactPoint.Email = virtualAddress1.VirtualAddressValue;
            }
            var virtualAddress2 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 2)
            .FirstOrDefaultAsync();
            if (virtualAddress2 != null) {
              dto.ContactPoint.WebUrl = virtualAddress2.VirtualAddressValue;
            }
            var virtualAddress3 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 3)
            .FirstOrDefaultAsync();
            if (virtualAddress3 != null) {
              dto.ContactPoint.PhoneNumber = virtualAddress3.VirtualAddressValue;
            }
            var virtualAddress4 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 4)
            .FirstOrDefaultAsync();
            if (virtualAddress4 != null) {
              dto.ContactPoint.Fax = virtualAddress4.VirtualAddressValue;
            }
          }
        }

        return dto;
      }
      return null;
    }

  }
}
