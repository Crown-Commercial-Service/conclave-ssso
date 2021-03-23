using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;

namespace CcsSso.Core.Tests.Infrastructure
{
  internal static class DtoHelper
  {
    public static ContactDetailDto GetContactDetailDto(int contactId, int partyId, int organisationId, ContactType contactType,
      string name, string email, string phone, Address address = null)
    {
      return new ContactDetailDto
      {
        ContactId = contactId,
        PartyId = partyId,
        OrganisationId = organisationId,
        ContactType = contactType,
        Name = name,
        Email = email,
        PhoneNumber = phone,
        Address = address
      };
    }

    public static ContactInfo GetContactInfo(string contactReason, string name, string email, string phoneNumber, string fax, string webUrl)
    {
      return new ContactInfo
      {
        ContactReason = contactReason,
        Name = name,
        Email = email,
        PhoneNumber = phoneNumber,
        Fax = fax,
        WebUrl = webUrl
      };
    }

    public static OrganisationProfileInfo GetOrganisationProfileInfo(string ciiOrganisationId, string name, string uri, string streetAddress, string locality,
      string region, string postalCode, string countryCode, bool isActive, bool isSme, bool isVcse, bool isNullIdentifier = false,
      bool isNullAddress = false, bool isNullDetails = false)
    {
      var organisationProfile = new OrganisationProfileInfo
      {
        OrganisationId = ciiOrganisationId
      };

      if (!isNullIdentifier)
      {
        organisationProfile.Identifier = new OrganisationIdentifier
        {
          LegalName = name,
          Uri = uri
        };
      }

      if (!isNullAddress)
      {
        organisationProfile.Address = new OrganisationAddress
        {
          StreetAddress = streetAddress,
          Locality = locality,
          Region = region,
          PostalCode = postalCode,
          CountryCode = countryCode
        };
      }

      if (!isNullDetails)
      {
        organisationProfile.Detail = new OrganisationDetail
        {
          IsActive = isActive,
          IsSme = isSme,
          IsVcse = isVcse
        };
      }

      return organisationProfile;
    }

    public static OrganisationSiteInfo GetOrganisationSiteInfo(string siteName, string streetAddress, string locality,
      string region, string postalCode, string countryCode)
    {
      return new OrganisationSiteInfo
      {
        SiteName = siteName,
        StreetAddress = streetAddress,
        Locality = locality,
        Region = region,
        PostalCode = postalCode,
        CountryCode = countryCode
      };
    }

    public static UserProfileResponseInfo GetUserProfileResponseInfo(string firstName, string lastName, string userName,
      string ccsOrganisationId, List<GroupAccessRole> groupAccessRoles)
    {
      return new UserProfileResponseInfo
      {
        FirstName = firstName,
        LastName = lastName,
        UserName = userName,
        OrganisationId = ccsOrganisationId,
        UserGroups = groupAccessRoles
      };
    }

    public static UserProfileRequestInfo GetUserProfileRequestInfo(string firstName, string lastName, string userName,
      string organisationId, int identityProviderId, List<int> groupIds)
    {
      return new UserProfileRequestInfo
      {
        FirstName = firstName,
        LastName = lastName,
        UserName = userName,
        OrganisationId = organisationId,
        IdentityProviderId = identityProviderId,
        GroupIds = groupIds
      };
    }

    public static GroupAccessRole GetGroupAccessRole(string groupName, string accessRoleName)
    {
      return new GroupAccessRole
      {
        Group = groupName,
        AccessRole = accessRoleName
      };
    }

    public static UserListResponse GetUserListResponse(string organisationId, int currentPage, int pageCount,
      int rowCount, List<UserListInfo> userList)
    {
      return new UserListResponse
      {
        OrganisationId = organisationId,
        CurrentPage = currentPage,
        PageCount = pageCount,
        RowCount = rowCount,
        UserList = userList
      };
    }

    public static UserListInfo GetUserListInfo(string name, string userName)
    {
      return new UserListInfo
      {
        Name = name,
        UserName = userName
      };
    }
  }
}
