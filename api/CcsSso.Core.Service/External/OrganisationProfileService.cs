using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class OrganisationProfileService : IOrganisationProfileService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    public OrganisationProfileService(IDataContext dataContext, IContactsHelperService contactsHelper)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
    }

    /// <summary>
    /// Create organisation profile and the physical address with OTHER contact reson
    /// </summary>
    /// <param name="organisationProfileInfo"></param>
    /// <returns></returns>
    public async Task<string> CreateOrganisationAsync(OrganisationProfileInfo organisationProfileInfo)
    {
      Validate(organisationProfileInfo);

      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.ExternalOrgnaisation)).Id;
      var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(ContactReasonType.Other);

      var organisation = new Organisation
      {
        CiiOrganisationId = organisationProfileInfo.OrganisationId,
        LegalName = organisationProfileInfo.Identifier.LegalName.Trim(),
        OrganisationUri = organisationProfileInfo.Identifier.Uri.Trim()
      };

      if (organisationProfileInfo.Detail != null)
      {
        organisation.IsSme = organisationProfileInfo.Detail.IsSme;
        organisation.IsVcse = organisationProfileInfo.Detail.IsVcse;
      }

      var contactPoint = new ContactPoint
      {
        PartyTypeId = partyTypeId,
        ContactPointReasonId = contactPointReasonId,
        ContactDetail = new ContactDetail
        {
          EffectiveFrom = DateTime.UtcNow,
          PhysicalAddress = new PhysicalAddress { }
        }
      };

      AssignPhysicalContactsToContactPoint(organisationProfileInfo.Address, contactPoint);

      var party = new Party
      {
        PartyTypeId = partyTypeId,
        Organisation = organisation,
        ContactPoints = new List<ContactPoint>()
      };

      party.ContactPoints.Add(contactPoint);

      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();

      return organisation.CiiOrganisationId;
    }

    //Deletion has commented out since its not going to be exposed via the api at the moment
    //public async Task DeleteOrganisationAsync(string ciiOrganisationId)
    //{
    //  List<string> userNames = new List<string>();

    //  var deletingOrganisation = await _dataContext.Organisation
    //    .Include(o => o.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
    //    .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

    //  if (deletingOrganisation != null)
    //  {
    //    deletingOrganisation.Party.IsDeleted = true;
    //    deletingOrganisation.IsDeleted = true;
    //    if (deletingOrganisation.Party.ContactPoints != null)
    //    {
    //      foreach (var orgContactPoint in deletingOrganisation.Party.ContactPoints)
    //      {
    //        orgContactPoint.IsDeleted = true;
    //        orgContactPoint.ContactDetail.IsDeleted = true;
    //        orgContactPoint.ContactDetail.PhysicalAddress.IsDeleted = true;
    //      }
    //    }

    //    var deletingOrganisationPeople = await _dataContext.Organisation
    //    .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.User)
    //    .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
    //    .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

    //    if (deletingOrganisationPeople.People != null)
    //    {
    //      foreach (var person in deletingOrganisationPeople.People)
    //      {
    //        person.Party.IsDeleted = true;

    //        if (person.Party.User != null)
    //        {
    //          person.Party.User.IsDeleted = true;
    //          userNames.Add(person.Party.User.UserName); // Add the userName to delete from Auth0
    //        }

    //        if (person.Party.ContactPoints != null)
    //        {
    //          foreach (var personContactPoint in person.Party.ContactPoints)
    //          {
    //            personContactPoint.IsDeleted = true;
    //            personContactPoint.ContactDetail.IsDeleted = true;

    //            if (personContactPoint.ContactDetail.VirtualAddresses != null)
    //            {
    //              foreach (var virtualContact in personContactPoint.ContactDetail.VirtualAddresses)
    //              {
    //                virtualContact.IsDeleted = true;
    //              }
    //            }
    //          }
    //        }
    //      }
    //    }

    //    //TODO delete other entities on Organisation

    //    await _dataContext.SaveChangesAsync();

    //  }
    //}

    /// <summary>
    /// Get the prganisation with its physical address
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <returns></returns>
    public async Task<OrganisationProfileInfo> GetOrganisationAsync(string ciiOrganisationId)
    {
      var organisation = await _dataContext.Organisation
        .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
        .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        var organisationInfo = new OrganisationProfileInfo
        {
          OrganisationId = organisation.CiiOrganisationId,
          Identifier = new OrganisationIdentifier
          {
            LegalName = organisation.LegalName,
            Uri = organisation.OrganisationUri,
          },
          Detail = new OrganisationDetail
          {
            IsActive = organisation.IsActivated,
            IsSme = organisation.IsSme,
            IsVcse = organisation.IsVcse,
            CreationDate = organisation.CreatedOnUtc.ToString(DateTimeFormat.DateFormat)
          }
        };

        if (organisation.Party.ContactPoints != null)
        {
          var contactPoint = organisation.Party.ContactPoints.OrderBy(cp => cp.CreatedOnUtc).FirstOrDefault();

          if (contactPoint != null)
          {
            var address = new OrganisationAddress
            {
              StreetAddress = contactPoint.ContactDetail.PhysicalAddress.StreetAddress ?? string.Empty,
              Region = contactPoint.ContactDetail.PhysicalAddress.Region ?? string.Empty,
              Locality = contactPoint.ContactDetail.PhysicalAddress.Locality ?? string.Empty,
              PostalCode = contactPoint.ContactDetail.PhysicalAddress.PostalCode ?? string.Empty,
              CountryCode = contactPoint.ContactDetail.PhysicalAddress.CountryCode ?? string.Empty,
            };
            organisationInfo.Address = address;
            organisationInfo.Detail.CountryCode = contactPoint.ContactDetail.PhysicalAddress.CountryCode;
          }
        }

        return organisationInfo;
      }

      throw new ResourceNotFoundException();
    }

    public async Task<List<OrganisationGroups>> GetOrganisationGroupsAsync(string ciiOrganisationId)
    {
      var organisation = await _dataContext.Organisation
       .Include(o => o.UserGroups)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var groups = organisation.UserGroups.Select(g => new OrganisationGroups
      {
        GroupId = g.Id,
        GroupName = g.UserGroupName
      }).ToList();

      return groups;
    }

    /// <summary>
    /// Update the organisation and its physical address
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="organisationProfileInfo"></param>
    /// <returns></returns>
    public async Task UpdateOrganisationAsync(string ciiOrganisationId, OrganisationProfileInfo organisationProfileInfo)
    {
      Validate(organisationProfileInfo);

      var organisation = await _dataContext.Organisation
       .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
       .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        organisation.LegalName = organisationProfileInfo.Identifier.LegalName.Trim();
        organisation.OrganisationUri = organisationProfileInfo.Identifier.Uri.Trim();

        if (organisationProfileInfo.Detail != null)
        {
          organisation.IsSme = organisationProfileInfo.Detail.IsSme;
          organisation.IsVcse = organisationProfileInfo.Detail.IsVcse;
        }

        if (organisationProfileInfo.Address != null && organisation.Party.ContactPoints != null)
        {
          var physicalAddressContactPoint = organisation.Party.ContactPoints.OrderBy(cp => cp.CreatedOnUtc).FirstOrDefault();

          if (physicalAddressContactPoint != null)
          {
            AssignPhysicalContactsToContactPoint(organisationProfileInfo.Address, physicalAddressContactPoint);
          }
        }

        await _dataContext.SaveChangesAsync();
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    private void AssignPhysicalContactsToContactPoint(OrganisationAddress addressDetails, ContactPoint contactPoint)
    {
      contactPoint.ContactDetail.PhysicalAddress.StreetAddress = addressDetails.StreetAddress;
      contactPoint.ContactDetail.PhysicalAddress.Region = addressDetails.Region;
      contactPoint.ContactDetail.PhysicalAddress.Locality = addressDetails.Locality;
      contactPoint.ContactDetail.PhysicalAddress.PostalCode = addressDetails.PostalCode;
      contactPoint.ContactDetail.PhysicalAddress.CountryCode = addressDetails.CountryCode;
    }

    private void Validate(OrganisationProfileInfo organisationProfileInfo)
    {
      if (organisationProfileInfo.Identifier == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidIdentifier);
      }

      if (string.IsNullOrWhiteSpace(organisationProfileInfo.Identifier.LegalName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidOrganisationName);
      }

      if (string.IsNullOrWhiteSpace(organisationProfileInfo.Identifier.Uri))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidOrganisationUri);
      }

      // TODO - finalize this in the Org wrapper review
      //if (organisationProfileInfo.Address == null || (string.IsNullOrWhiteSpace(organisationProfileInfo.Address.StreetAddress) && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.Locality)
      //  && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.Region) && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.PostalCode)
      //  && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.CountryCode)))
      //{
      //  throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      //}
    }

  }
}
