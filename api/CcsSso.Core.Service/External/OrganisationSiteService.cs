using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class OrganisationSiteService : IOrganisationSiteService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly IAuditLoginService _auditLoginService;

    public OrganisationSiteService(IDataContext dataContext, IContactsHelperService contactsHelper, IWrapperCacheService wrapperCacheService,
      IAuditLoginService auditLoginService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _wrapperCacheService = wrapperCacheService;
      _auditLoginService = auditLoginService;
    }

    /// <summary>
    /// Create organisation site
    /// Site is another contact point with isSite value true and with a siteName
    /// This conatact point include the site's physical contact
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="organisationSiteInfo"></param>
    /// <returns></returns>
    public async Task<int> CreateSiteAsync(string ciiOrganisationId, OrganisationSiteInfo organisationSiteInfo)
    {
      Validate(organisationSiteInfo);

      //Validate duplication  Sitename - Create Site
      var Prev_SiteInfo = _dataContext.ContactPoint
        .Include(cp => cp.ContactPointReason)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Where(cp => !cp.IsDeleted && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId
        && cp.SiteName == organisationSiteInfo.SiteName).FirstOrDefault();

      if (Prev_SiteInfo != null)
      {
        if (organisationSiteInfo.SiteName.Trim().ToString() == Prev_SiteInfo.SiteName.ToString())
        {
          throw new ResourceAlreadyExistsException();
        }
      }

      var organisation = await _dataContext.Organisation
       .Include(o => o.Party)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(ContactReasonType.Site);

        var contactPoint = new ContactPoint
        {
          PartyId = organisation.PartyId,
          PartyTypeId = organisation.Party.PartyTypeId,
          ContactPointReasonId = contactPointReasonId,
          IsSite = true,
          SiteName = organisationSiteInfo.SiteName,
          ContactDetail = new ContactDetail
          {
            EffectiveFrom = DateTime.UtcNow,
            PhysicalAddress = new PhysicalAddress { }
          }
        };

        AssignSitePhysicalContactsDetailsToContactPoint(organisationSiteInfo, contactPoint);

        _dataContext.ContactPoint.Add(contactPoint);

        await _dataContext.SaveChangesAsync();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgSiteCreate, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, SiteId:{contactPoint.Id}");

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrgSites}-{ciiOrganisationId}");

        return contactPoint.Id;
      }

      throw new ResourceNotFoundException();
    }

    /// <summary>
    /// Delete site and its physical address and its contacts
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteId"></param>
    /// <returns></returns>
    public async Task DeleteSiteAsync(string ciiOrganisationId, int siteId)
    {
      var organisationSiteContactPoint = await _dataContext.ContactPoint
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Include(cp => cp.SiteContacts)
        .Where(cp => !cp.IsDeleted && cp.Id == siteId && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
        .FirstOrDefaultAsync();

      if (organisationSiteContactPoint != null)
      {
        organisationSiteContactPoint.IsDeleted = true;

        organisationSiteContactPoint.ContactDetail.IsDeleted = true;

        if (organisationSiteContactPoint.ContactDetail.PhysicalAddress != null)
        {
          organisationSiteContactPoint.ContactDetail.PhysicalAddress.IsDeleted = true;
        }

        if (organisationSiteContactPoint.SiteContacts != null)
        {
          organisationSiteContactPoint.SiteContacts.ForEach((siteContact) =>
          {
            siteContact.IsDeleted = true;
          });
        }

        await _dataContext.SaveChangesAsync();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgSiteDelete, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, SiteId:{siteId}");

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrgSites}-{ciiOrganisationId}", $"{CacheKeyConstant.Site}-{ciiOrganisationId}-{siteId}");
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Get all the sites in organisation or search sites of an organisation by site name
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteNameSerachString"></param>
    /// <returns></returns>
    public async Task<OrganisationSiteInfoList> GetOrganisationSitesAsync(string ciiOrganisationId, string siteNameSerachString = null)
    {
      var organisationSiteContactPoints = await _dataContext.ContactPoint
        .Include(cp => cp.ContactPointReason)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Where(cp => !cp.IsDeleted && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId
          && (string.IsNullOrWhiteSpace(siteNameSerachString) || cp.SiteName.ToLower().Contains(siteNameSerachString.Trim().ToLower())))
        .ToListAsync();

      if (!organisationSiteContactPoints.Any() && !await _dataContext.Organisation.AnyAsync(o => o.CiiOrganisationId == ciiOrganisationId))
      {
        throw new ResourceNotFoundException();
      }

      var sites = new List<OrganisationSite>();

      foreach (var organisationSiteContactPoint in organisationSiteContactPoints)
      {
        var organisationSite = new OrganisationSite
        {
          Details = new SiteDetail
          {
            SiteId = organisationSiteContactPoint.Id,
          },
          SiteName = organisationSiteContactPoint.SiteName,
          Address = new OrganisationAddressResponse
          {
            StreetAddress = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.StreetAddress ?? string.Empty,
            Locality = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Locality ?? string.Empty,
            Region = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Region ?? string.Empty,
            PostalCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.PostalCode ?? string.Empty,
            CountryCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.CountryCode ?? string.Empty,
          }
        };

        if (!string.IsNullOrEmpty(organisationSiteContactPoint.ContactDetail.PhysicalAddress?.CountryCode))
        {
          organisationSite.Address.CountryName = CultureSupport.GetCountryNameByCode(organisationSiteContactPoint.ContactDetail.PhysicalAddress.CountryCode);
        }
        sites.Add(organisationSite);
      }

      return new OrganisationSiteInfoList
      {
        OrganisationId = ciiOrganisationId,
        Sites = sites
      };
    }

    /// <summary>
    /// Get site by id
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteId"></param>
    /// <returns></returns>
    public async Task<OrganisationSiteResponse> GetSiteAsync(string ciiOrganisationId, int siteId)
    {
      var organisationSiteContactPoint = await _dataContext.ContactPoint
        .Include(cp => cp.ContactPointReason)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Where(cp => !cp.IsDeleted && cp.Id == siteId && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
        .FirstOrDefaultAsync();

      if (organisationSiteContactPoint != null)
      {
        var organisationSiteResponse = new OrganisationSiteResponse
        {
          OrganisationId = ciiOrganisationId,
          Details = new SiteDetail
          {
            SiteId = siteId
          },
          SiteName = organisationSiteContactPoint.SiteName,
          Address = new OrganisationAddressResponse
          {
            StreetAddress = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.StreetAddress ?? string.Empty,
            Locality = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Locality ?? string.Empty,
            Region = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Region ?? string.Empty,
            PostalCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.PostalCode ?? string.Empty,
            CountryCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.CountryCode ?? string.Empty,
          }
        };

        if (!string.IsNullOrEmpty(organisationSiteContactPoint.ContactDetail.PhysicalAddress?.CountryCode))
        {
          organisationSiteResponse.Address.CountryName = CultureSupport.GetCountryNameByCode(organisationSiteContactPoint.ContactDetail.PhysicalAddress.CountryCode);
        }

        return organisationSiteResponse;
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Update the site. This updates the site name and contac reason and the physical address
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="organisationSiteInfo"></param>
    /// <returns></returns>
    public async Task UpdateSiteAsync(string ciiOrganisationId, int siteId, OrganisationSiteInfo organisationSiteInfo)
    {
      Validate(organisationSiteInfo);

      //Validate duplication  Sitename - Update Site
      var Prev_SiteInfo = _dataContext.ContactPoint
        .Include(cp => cp.ContactPointReason)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Where(cp => !cp.IsDeleted && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId
        && cp.SiteName == organisationSiteInfo.SiteName && cp.Id != siteId).FirstOrDefault();

      if (Prev_SiteInfo != null)
      {
        if (Prev_SiteInfo.SiteName.Trim().ToString() == organisationSiteInfo.SiteName.ToString())
        {
          throw new ResourceAlreadyExistsException();
        }
      }

      var organisationSiteContactPoint = await _dataContext.ContactPoint
     .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
     .Where(cp => !cp.IsDeleted && cp.Id == siteId && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
     .FirstOrDefaultAsync();

      if (organisationSiteContactPoint != null)
      {
        var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(ContactReasonType.Site);

        organisationSiteContactPoint.SiteName = organisationSiteInfo.SiteName;
        organisationSiteContactPoint.ContactPointReasonId = contactPointReasonId;

        AssignSitePhysicalContactsDetailsToContactPoint(organisationSiteInfo, organisationSiteContactPoint);

        await _dataContext.SaveChangesAsync();

        // Log
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgSiteUpdate, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, SiteId:{siteId}");

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrgSites}-{ciiOrganisationId}", $"{CacheKeyConstant.Site}-{ciiOrganisationId}-{siteId}");
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Private common method used in both create and update site methods to assign physical contact values to contact point
    /// </summary>
    /// <param name="organisationSiteInfo"></param>
    /// <param name="contactPoint"></param>
    private void AssignSitePhysicalContactsDetailsToContactPoint(OrganisationSiteInfo organisationSiteInfo, ContactPoint contactPoint)
    {
      contactPoint.ContactDetail.PhysicalAddress.StreetAddress = organisationSiteInfo.Address.StreetAddress;
      contactPoint.ContactDetail.PhysicalAddress.Region = organisationSiteInfo.Address.Region;
      contactPoint.ContactDetail.PhysicalAddress.Locality = organisationSiteInfo.Address.Locality;
      contactPoint.ContactDetail.PhysicalAddress.PostalCode = organisationSiteInfo.Address.PostalCode;
      contactPoint.ContactDetail.PhysicalAddress.CountryCode = organisationSiteInfo.Address.CountryCode;
    }

    /// <summary>
    /// Retrieves CountryName based on country code
    /// </summary>
    /// <returns></returns>
    public string GetCountryNameByCode(string countyCode)
    {
      try
      {
        string CountryName = string.Empty;
        if (!string.IsNullOrEmpty(countyCode))
        {
          CountryName = _dataContext.CountryDetails.FirstOrDefault(x => x.IsDeleted == false && x.Code == countyCode)?.Name;
        }
        return CountryName;
      }
      catch (ArgumentException)
      {
      }
      return null;
    }

    /// <summary>
    /// Validate
    /// </summary>
    /// <param name="organisationSiteInfo"></param>
    private void Validate(OrganisationSiteInfo organisationSiteInfo)
    {
      if (string.IsNullOrWhiteSpace(organisationSiteInfo.SiteName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidSiteName);
      }

      if (organisationSiteInfo.Address == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidSiteAddress);
      }

      if (organisationSiteInfo.Address != null)
      {
        if (string.IsNullOrWhiteSpace(organisationSiteInfo.Address.CountryCode))
        {
          throw new CcsSsoException(ErrorConstant.ErrorCountyRequired);
        }
      }

      if (string.IsNullOrWhiteSpace(organisationSiteInfo.Address.StreetAddress) || string.IsNullOrWhiteSpace(organisationSiteInfo.Address.PostalCode)
        || string.IsNullOrWhiteSpace(organisationSiteInfo.Address.CountryCode))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      }

      if (!UtilityHelper.IsSiteNameValid(organisationSiteInfo.SiteName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidSiteName);
      }

      if (!UtilityHelper.IsStreetAddressValid(organisationSiteInfo.Address.StreetAddress.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidStreetAddress);
      }

      if (!UtilityHelper.IslocalityValid(organisationSiteInfo.Address.Locality.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidlocality);
      }

      string CountryName = String.Empty;
      if (!string.IsNullOrEmpty(organisationSiteInfo.Address.CountryCode))
      {
        CountryName = GetCountryNameByCode(organisationSiteInfo.Address.CountryCode);
      }
      if (!string.IsNullOrWhiteSpace(organisationSiteInfo.Address.CountryCode) && string.IsNullOrWhiteSpace(CountryName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCountryCode);
      }
    }
  }
}
