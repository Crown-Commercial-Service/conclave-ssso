using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Exceptions;
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
    public OrganisationSiteService(IDataContext dataContext, IContactsHelperService contactsHelper)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
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
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    /// <summary>
    /// Get all the sites in organisation
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <returns></returns>
    public async Task<OrganisationSiteInfoList> GetOrganisationSitesAsync(string ciiOrganisationId)
    {
      var organisationSiteContactPoints = await _dataContext.ContactPoint
        .Include(cp => cp.ContactPointReason)
        .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .Where(cp => !cp.IsDeleted && cp.IsSite && cp.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
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
          SiteId = organisationSiteContactPoint.Id,
          SiteName = organisationSiteContactPoint.SiteName,
          StreetAddress = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.StreetAddress ?? string.Empty,
          Locality = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Locality ?? string.Empty,
          Region = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Region ?? string.Empty,
          PostalCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.PostalCode ?? string.Empty,
          CountryCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.CountryCode ?? string.Empty,
        };

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
          SiteId = siteId,
          SiteName = organisationSiteContactPoint.SiteName,
          StreetAddress = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.StreetAddress ?? string.Empty,
          Locality = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Locality ?? string.Empty,
          Region = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.Region ?? string.Empty,
          PostalCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.PostalCode ?? string.Empty,
          CountryCode = organisationSiteContactPoint.ContactDetail.PhysicalAddress?.CountryCode ?? string.Empty,
        };

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
      contactPoint.ContactDetail.PhysicalAddress.StreetAddress = organisationSiteInfo.StreetAddress;
      contactPoint.ContactDetail.PhysicalAddress.Region = organisationSiteInfo.Region;
      contactPoint.ContactDetail.PhysicalAddress.Locality = organisationSiteInfo.Locality;
      contactPoint.ContactDetail.PhysicalAddress.PostalCode = organisationSiteInfo.PostalCode;
      contactPoint.ContactDetail.PhysicalAddress.CountryCode = organisationSiteInfo.CountryCode;
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

      if (string.IsNullOrWhiteSpace(organisationSiteInfo.StreetAddress) && string.IsNullOrWhiteSpace(organisationSiteInfo.Locality)
        && string.IsNullOrWhiteSpace(organisationSiteInfo.Region) && string.IsNullOrWhiteSpace(organisationSiteInfo.PostalCode)
        && string.IsNullOrWhiteSpace(organisationSiteInfo.CountryCode))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      }
    }
  }
}
