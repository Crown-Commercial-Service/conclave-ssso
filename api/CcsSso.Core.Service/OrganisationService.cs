using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Domain.Contexts;
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
    private readonly ICiiService _ciiService;
    private readonly IOrganisationProfileService _organisationProfileService;
    private readonly IUserProfileService _userProfileService;
    private readonly IOrganisationContactService _organisationContactService;
    private readonly RequestContext _requestContext;
    public OrganisationService(IDataContext dataContext, IAdaptorNotificationService adapterNotificationService,
      IWrapperCacheService wrapperCacheService, ICiiService ciiService, IOrganisationProfileService organisationProfileService,
      IUserProfileService userProfileService, IOrganisationContactService organisationContactService, RequestContext requestContext)
    {
      _dataContext = dataContext;
      _adapterNotificationService = adapterNotificationService;
      _wrapperCacheService = wrapperCacheService;
      _ciiService = ciiService;
      _organisationProfileService = organisationProfileService;
      _userProfileService = userProfileService;
      _organisationContactService = organisationContactService;
      _requestContext = requestContext;
    }

    /// <summary>
    /// Register an organisation
    /// First create CII record
    /// Then Organisation in DB
    /// Then Admin user
    /// Finally if any contacts
    /// </summary>
    /// <param name="organisationRegistrationDto"></param>
    /// <returns></returns>
    public async Task<string> RegisterAsync(OrganisationRegistrationDto organisationRegistrationDto)
    {
      var ciiOrgId = await _ciiService.PostAsync(organisationRegistrationDto.CiiDetails);
      await CreateOrganisationAsync(organisationRegistrationDto, ciiOrgId);
      await CreateAdminUserAsync(organisationRegistrationDto, ciiOrgId);

      if (organisationRegistrationDto.CiiDetails.ContactPoint != null &&
        (!string.IsNullOrEmpty(organisationRegistrationDto.CiiDetails.ContactPoint.Name) || !string.IsNullOrEmpty(organisationRegistrationDto.CiiDetails.ContactPoint.Email) ||
        !string.IsNullOrEmpty(organisationRegistrationDto.CiiDetails.ContactPoint.Telephone) || !string.IsNullOrEmpty(organisationRegistrationDto.CiiDetails.ContactPoint.FaxNumber) ||
        !string.IsNullOrEmpty(organisationRegistrationDto.CiiDetails.ContactPoint.Uri)))
      {
        await CreateOrganisationContactAsync(organisationRegistrationDto, ciiOrgId);
      }

      return ciiOrgId;
    }

    /// <summary>
    /// Delete the organisation from CII and DB
    /// </summary>
    /// <param name="ciiOrgId"></param>
    /// <returns></returns>
    public async Task DeleteAsync(string ciiOrgId)
    {
      await _ciiService.DeleteOrgAsync(ciiOrgId);

      var organisation = await _dataContext.Organisation
        .Include(o => o.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Include(o => o.OrganisationEligibleRoles)
        .Include(o => o.OrganisationEligibleIdentityProviders)
        .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrgId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      organisation.IsDeleted = true;
      organisation.Party.IsDeleted = true;
      organisation.Party.ContactPoints.ForEach((cp) =>
      {
        cp.IsDeleted = true;
        cp.ContactDetail.IsDeleted = true;
        cp.ContactDetail.PhysicalAddress.IsDeleted = true;
      });
      organisation.OrganisationEligibleRoles.ForEach(oer => oer.IsDeleted = true);
      organisation.OrganisationEligibleIdentityProviders.ForEach(oeip => oeip.IsDeleted = true);

      await _dataContext.SaveChangesAsync();
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
          SupplierBuyerType = organisation.SupplierBuyerType ?? 0
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

    public async Task<List<OrganisationUserDto>> GetUsersAsync(string name)
    {
      name = name?.ToLower();
      var users = await _dataContext.User
        .Include(c => c.Party)
        .ThenInclude(x => x.Person)
        .ThenInclude(o => o.Organisation)
        .Where(u => u.IsDeleted == false && (_requestContext.UserId != 0 && u.Party.Person.Organisation.CiiOrganisationId != _requestContext.CiiOrganisationId) &&
        (string.IsNullOrEmpty(name) || u.UserName.Contains(name) || (u.Party.Person.FirstName + " " + u.Party.Person.LastName).ToLower().Contains(name) || u.Party.Person.Organisation.LegalName.ToLower().Contains(name)) &&
        u.Party.Person.Organisation.IsDeleted == false).Select(user => new OrganisationUserDto
        {
          Id = user.Id,
          UserName = user.UserName,
          Name = user.Party.Person.FirstName + " " + user.Party.Person.LastName,
          OrganisationId = user.Party.Person.Organisation.Id,
          OrganisationLegalName = user.Party.Person.Organisation.LegalName,
          CiiOrganisationId = user.Party.Person.Organisation.CiiOrganisationId
        }).ToListAsync();
      return users;
    }

    /// <summary>
    /// Create organisation in DB using wrapper
    /// If failed delete the CII record
    /// </summary>
    /// <param name="organisationRegistrationDto"></param>
    /// <param name="ciiOrgId"></param>
    /// <returns></returns>
    private async Task CreateOrganisationAsync(OrganisationRegistrationDto organisationRegistrationDto, string ciiOrgId)
    {
      try
      {
        OrganisationProfileInfo organisationProfileInfo = new OrganisationProfileInfo
        {
          Identifier = new OrganisationIdentifier
          {
            Id = organisationRegistrationDto.CiiDetails.Identifier.Id,
            LegalName = organisationRegistrationDto.CiiDetails.Identifier.LegalName,
            Uri = organisationRegistrationDto.CiiDetails.Identifier.Uri,
            Scheme = organisationRegistrationDto.CiiDetails.Identifier.Scheme
          },
          Address = organisationRegistrationDto.CiiDetails.Address != null ? new OrganisationAddress
          {
            StreetAddress = organisationRegistrationDto.CiiDetails.Address.StreetAddress,
            Locality = organisationRegistrationDto.CiiDetails.Address.Locality,
            PostalCode = organisationRegistrationDto.CiiDetails.Address.PostalCode,
            Region = organisationRegistrationDto.CiiDetails.Address.Region,
          } : null,
          Detail = new OrganisationDetail
          {
            OrganisationId = ciiOrgId,
            BusinessType = organisationRegistrationDto.BusinessType,
            RightToBuy = organisationRegistrationDto.RightToBuy,
            SupplierBuyerType = organisationRegistrationDto.SupplierBuyerType,
          }
        };

        await _organisationProfileService.CreateOrganisationAsync(organisationProfileInfo);
      }
      catch (Exception)
      {
        await _ciiService.DeleteOrgAsync(ciiOrgId);
        throw;
      }
    }

    /// <summary>
    /// Create organisation admin user using wrapper
    /// If failed delete org records from CII and DB
    /// </summary>
    /// <param name="organisationRegistrationDto"></param>
    /// <param name="ciiOrgId"></param>
    /// <returns></returns>
    private async Task CreateAdminUserAsync(OrganisationRegistrationDto organisationRegistrationDto, string ciiOrgId)
    {
      try
      {
        var identifyProvider = _dataContext.OrganisationEligibleIdentityProvider.FirstOrDefault(oip =>
        oip.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName && oip.Organisation.CiiOrganisationId == ciiOrgId);
        var adminRole = await _dataContext.OrganisationEligibleRole
          .FirstOrDefaultAsync(r => r.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey && r.Organisation.CiiOrganisationId == ciiOrgId);
        UserProfileEditRequestInfo userProfileEditRequestInfo = new UserProfileEditRequestInfo
        {
          OrganisationId = ciiOrgId,
          FirstName = organisationRegistrationDto.AdminUserFirstName,
          LastName = organisationRegistrationDto.AdminUserLastName,
          UserName = organisationRegistrationDto.AdminUserName,
          MfaEnabled = true,
          Detail = new UserRequestDetail
          {
            IdentityProviderId = identifyProvider.Id,
            RoleIds = new List<int> { adminRole.Id },
          }
        };

        await _userProfileService.CreateUserAsync(userProfileEditRequestInfo);
      }
      catch (ResourceAlreadyExistsException)
      {
        await DeleteAsync(ciiOrgId);
        throw new CcsSsoException("ERROR_USER_ALREADY_EXISTS");
      }
      catch (Exception)
      {
        await DeleteAsync(ciiOrgId);
        throw;
      }
    }

    /// <summary>
    /// Create organisation contact using wrapper
    /// If failed delete org records from CII and DB and dlete the user record
    /// </summary>
    /// <param name="organisationRegistrationDto"></param>
    /// <param name="ciiOrgId"></param>
    /// <returns></returns>
    private async Task CreateOrganisationContactAsync(OrganisationRegistrationDto organisationRegistrationDto, string ciiOrgId)
    {
      try
      {
        var contactPoint = organisationRegistrationDto.CiiDetails.ContactPoint;
        ContactRequestInfo contactRequestInfo = new ContactRequestInfo
        {
          ContactPointName = contactPoint.Name,
          Contacts = new List<ContactRequestDetail>()
        };

        if (!string.IsNullOrEmpty(contactPoint.Email))
        {
          contactRequestInfo.Contacts.Add(new ContactRequestDetail { ContactType = VirtualContactTypeName.Email, ContactValue = contactPoint.Email });
        }
        if (!string.IsNullOrEmpty(contactPoint.Telephone))
        {
          contactRequestInfo.Contacts.Add(new ContactRequestDetail { ContactType = VirtualContactTypeName.Phone, ContactValue = contactPoint.Telephone });
        }
        if (!string.IsNullOrEmpty(contactPoint.FaxNumber))
        {
          contactRequestInfo.Contacts.Add(new ContactRequestDetail { ContactType = VirtualContactTypeName.Fax, ContactValue = contactPoint.FaxNumber });
        }
        if (!string.IsNullOrEmpty(contactPoint.Uri))
        {
          contactRequestInfo.Contacts.Add(new ContactRequestDetail { ContactType = VirtualContactTypeName.Url, ContactValue = contactPoint.Uri });
        }

        await _organisationContactService.CreateOrganisationContactAsync(ciiOrgId, contactRequestInfo);
      }
      catch (Exception)
      {
        await DeleteAsync(ciiOrgId);
        await _userProfileService.DeleteUserAsync(organisationRegistrationDto.AdminUserName, false);
        throw;
      }
    }
  }
}
