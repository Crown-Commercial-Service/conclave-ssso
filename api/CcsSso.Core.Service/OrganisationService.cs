using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Services.Helpers;
using CcsSso.Shared.Domain.Constants;
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
    private readonly IAdaptorNotificationService _adapterNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    public OrganisationService(IDataContext dataContext, IAdaptorNotificationService adapterNotificationService,
      IWrapperCacheService wrapperCacheService)
    {
      _dataContext = dataContext;
      _adapterNotificationService = adapterNotificationService;
      _wrapperCacheService = wrapperCacheService;
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
        CreatedUserId = 0,
        LastUpdatedUserId = 0,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();
      var org = new Organisation
      {
        CiiOrganisationId = model.CiiOrganisationId,
        OrganisationUri = model.OrganisationUri,
        LegalName = model.LegalName,
        RightToBuy = model.RightToBuy,
        BusinessType = model.BusinessType,
        SupplierBuyerType = model.SupplierBuyerType,
        PartyId = party.Id,
        CreatedUserId = party.Id,
        LastUpdatedUserId = party.Id,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.Organisation.Add(org);

      var eligibleRoles = await GetOrganisationEligibleRolesAsync(org, model.SupplierBuyerType);

      _dataContext.OrganisationEligibleRole.AddRange(eligibleRoles);

      var listEligibleProviders = new List<OrganisationEligibleIdentityProvider>();
      var providers = await _dataContext.IdentityProvider.Where(x => x.IsDeleted == false).ToListAsync();
      providers.ForEach((idp) =>
      {
        var p = new OrganisationEligibleIdentityProvider
        {
          IdentityProvider = idp,
          Organisation = org
        };
        listEligibleProviders.Add(p);
      });
      _dataContext.OrganisationEligibleIdentityProvider.AddRange(listEligibleProviders);
      await _dataContext.SaveChangesAsync();

      // Notify the adapter
      await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Create, org.CiiOrganisationId);

      return org.Id;
    }
    //var role = await _dataContext.CcsAccessRole.FirstOrDefaultAsync(x => x.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");
    //var orgEligibleRoleAdmin = new OrganisationEligibleRole
    //{
    //  CcsAccessRole = role,
    //  Organisation = org
    //};
    //_dataContext.OrganisationEligibleRole.Add(orgEligibleRoleAdmin);
    //var role2 = await _dataContext.CcsAccessRole.FirstOrDefaultAsync(x => x.CcsAccessRoleNameKey == "ORG_DEFAULT_USER");
    //var orgEligibleRoleUser = new OrganisationEligibleRole
    //{
    //  CcsAccessRole = role2,
    //  Organisation = org
    //};
    //_dataContext.OrganisationEligibleRole.Add(orgEligibleRoleUser);
    /// <summary>
    /// Delete an organisation by its id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(int id)
    {
      var organisation = await _dataContext.Organisation
        .Where(x => x.Id == id)
        .Include(c => c.Party)
        .FirstOrDefaultAsync();
      if (organisation != null)
      {
        organisation.IsDeleted = true;
        if (organisation.Party != null)
        {
          organisation.Party.IsDeleted = true;
        }
        await _dataContext.SaveChangesAsync();

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.Organisation}-{organisation.CiiOrganisationId}");

        // Notify the adapter
        await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Delete, organisation.CiiOrganisationId);
      }

      //var group = await _dataContext.OrganisationUserGroup
      //  .Where(x => x.Organisation.Id == id)
      //  .Include(o => o.Organisation)
      //  .ToListAsync();
      //if (group != null && group.Any())
      //{
      //  group.ForEach((g) =>
      //  {
      //    g.IsDeleted = true;
      //  });
      //  await _dataContext.SaveChangesAsync();
      //}

      var eRoles = await _dataContext.OrganisationEligibleRole
        .Where(x => x.Organisation.Id == id)
        .Include(o => o.Organisation)
        .ToListAsync();
      if (eRoles != null && eRoles.Any())
      {
        eRoles.ForEach((e) =>
        {
          e.IsDeleted = true;
        });
        await _dataContext.SaveChangesAsync();
      }

      var idpProviders = await _dataContext.OrganisationEligibleIdentityProvider
        .Where(x => x.Organisation.Id == id)
        .Include(o => o.Organisation)
        .ToListAsync();
      if (idpProviders != null && idpProviders.Any())
      {
        idpProviders.ForEach((e) =>
        {
          e.IsDeleted = true;
        });
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
      if (organisation != null)
      {
        var dto = new OrganisationDto
        {
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
          if (contactDetail != null)
          {
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
            dto.ContactPoint = new ContactDetailDto
            {
              Email = "",
              WebUrl = "",
              PhoneNumber = "",
              Fax = ""
            };
            var virtualAddress1 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 1)
            .FirstOrDefaultAsync();
            if (virtualAddress1 != null)
            {
              dto.ContactPoint.Email = virtualAddress1.VirtualAddressValue;
            }
            var virtualAddress2 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 2)
            .FirstOrDefaultAsync();
            if (virtualAddress2 != null)
            {
              dto.ContactPoint.WebUrl = virtualAddress2.VirtualAddressValue;
            }
            var virtualAddress3 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 3)
            .FirstOrDefaultAsync();
            if (virtualAddress3 != null)
            {
              dto.ContactPoint.PhoneNumber = virtualAddress3.VirtualAddressValue;
            }
            var virtualAddress4 = await _dataContext.VirtualAddress
            .Where(x => x.ContactDetailId == contactDetail.Id && x.VirtualAddressTypeId == 4)
            .FirstOrDefaultAsync();
            if (virtualAddress4 != null)
            {
              dto.ContactPoint.Fax = virtualAddress4.VirtualAddressValue;
            }
          }
        }

        return dto;
      }
      return null;
    }

    public async Task<List<OrganisationDto>> GetAllAsync(string orgName)
    {
      var organisations = await _dataContext.Organisation
        .Where(x => x.IsDeleted == false && (string.IsNullOrEmpty(orgName) || x.LegalName.ToLower().Contains(orgName.ToLower())))
        .Select(organisation => new OrganisationDto
        {
          OrganisationId = organisation.Id,
          CiiOrganisationId = organisation.CiiOrganisationId,
          OrganisationUri = organisation.OrganisationUri,
          RightToBuy = organisation.RightToBuy,
          PartyId = organisation.PartyId,
          LegalName = organisation.LegalName,
        }).ToListAsync();

      return organisations;
    }

    /// <summary>
    /// Updates an organisation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task PutAsync(OrganisationDto model)
    {
      var organisation = await _dataContext.Organisation
        .Where(x => x.CiiOrganisationId == model.CiiOrganisationId && x.IsDeleted == false)
        .FirstOrDefaultAsync();
      if (organisation != null)
      {
        organisation.RightToBuy = model.RightToBuy;
        await _dataContext.SaveChangesAsync();

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.Organisation}-{organisation.CiiOrganisationId}");

        // Notify the adapter
        await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Update, organisation.CiiOrganisationId);
      }
    }

    public async Task Rollback(OrganisationRollbackDto model)
    {
      try
      {

      }
      catch (Exception ex)
      {
        Console.Write(ex);
      }
    }

    public async Task<List<OrganisationUserDto>> GetUsersAsync(string name)
    {
      name = name?.ToLower();
      var users = await _dataContext.User
        .Include(c => c.Party)
        .ThenInclude(x => x.Person)
        .ThenInclude(o => o.Organisation)
        .Where(u => u.IsDeleted == false &&
        (string.IsNullOrEmpty(name) || u.UserName.Contains(name) || (u.Party.Person.FirstName + " " + u.Party.Person.LastName).ToLower().Contains(name) || u.Party.Person.Organisation.LegalName.ToLower().Contains(name)) &&
        u.Party.Person.Organisation.IsDeleted == false).Select(user => new OrganisationUserDto
        {
          Id = user.Id,
          UserName = user.UserName,
          Name = user.Party.Person.FirstName + " " + user.Party.Person.LastName,
          OrganisationId = user.Party.Person.Organisation.Id,
          OrganisationLegalName = user.Party.Person.Organisation.LegalName,
        }).ToListAsync();
      return users;
    }

    public async Task<List<OrganisationEligibleRole>> GetOrganisationEligibleRolesAsync(Organisation org, int supplierBuyerType)
    {
      var eligibleRoles = new List<OrganisationEligibleRole>();
      if (supplierBuyerType == 0)
      {
        var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Supplier || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToListAsync();
        roles.ForEach((role) =>
        {
          var eligibleRole = new OrganisationEligibleRole
          {
            CcsAccessRole = role,
            Organisation = org
          };
          eligibleRoles.Add(eligibleRole);
        });
      }
      else if (supplierBuyerType == 1)
      {
        var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Buyer || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToListAsync();
        roles.ForEach((role) =>
        {
          var eligibleRole = new OrganisationEligibleRole
          {
            CcsAccessRole = role,
            Organisation = org
          };
          eligibleRoles.Add(eligibleRole);
        });
      }
      else
      {
        var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Supplier || ar.TradeEligibility == RoleEligibleTradeType.Buyer || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToListAsync();
        roles.ForEach((role) =>
        {
          var eligibleRole = new OrganisationEligibleRole
          {
            CcsAccessRole = role,
            Organisation = org
          };
          eligibleRoles.Add(eligibleRole);
        });
      }

      return eligibleRoles;
    }
  }
}
