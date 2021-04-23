using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using System;
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
        Detail = new OrganisationDetail
        {
          OrganisationId = ciiOrganisationId
        }
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
        organisationProfile.Detail.IsActive = isActive;
        organisationProfile.Detail.IsSme = isSme;
        organisationProfile.Detail.IsVcse = isVcse;
      }

      return organisationProfile;
    }

    public static OrganisationSiteInfo GetOrganisationSiteInfo(string siteName, string streetAddress, string locality,
      string region, string postalCode, string countryCode)
    {
      return new OrganisationSiteInfo
      {
        SiteName = siteName,
        Address = new OrganisationAddress
        {
          StreetAddress = streetAddress,
          Locality = locality,
          Region = region,
          PostalCode = postalCode,
          CountryCode = countryCode
        }
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
        Detail = new UserResponseDetail
        {
          UserGroups = groupAccessRoles
        },
        OrganisationId = ccsOrganisationId
      };
    }

    public static UserProfileEditRequestInfo GetUserProfileRequestInfo(string firstName, string lastName, string userName,
      string organisationId, int identityProviderId, UserTitle? title, List<int> groupIds = null, List<int> roleIds = null)
    {
      return new UserProfileEditRequestInfo
      {
        FirstName = firstName,
        LastName = lastName,
        UserName = userName,
        OrganisationId = organisationId,
        Title = title,
        Detail = new UserRequestDetail
        {
          IdentityProviderId = identityProviderId,
          GroupIds = groupIds,
          RoleIds = roleIds
        }
      };
    }

    public static GroupAccessRole GetGroupAccessRole(string groupName, string accessRoleKey, string accessRoleName)
    {
      return new GroupAccessRole
      {
        Group = groupName,
        AccessRole = accessRoleKey,
        AccessRoleName = accessRoleName
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

    public static OrganisationGroupNameInfo GetOrganisationGroupNameInfo(string name)
    {
      return new OrganisationGroupNameInfo
      {
        GroupName = name
      };
    }

    public static OrganisationGroupResponseInfo GetOrganisationGroupResponse(string orgId, int groupId, string groupName,
      List<GroupRole> groupRoles, List<GroupUser> groupUsers, DateTime dateTime)
    {
      return new OrganisationGroupResponseInfo
      {
        OrganisationId = orgId,
        GroupId = groupId,
        GroupName = groupName,
        Roles = groupRoles,
        Users = groupUsers,
        CreatedDate = dateTime.ToString(DateTimeFormat.DateFormatShortMonth)
      };
    }

    public static GroupRole GetGroupRole(int id, string name)
    {
      return new GroupRole
      {
        Id = id,
        Name = name
      };
    }

    public static GroupUser GetGroupUser(string userName, string name)
    {
      return new GroupUser
      {
        UserId = userName,
        Name = name
      };
    }

    public static OrganisationGroupList GetOrganisationGroupListObject(string orgId, List<OrganisationGroupInfo> organisationGroupInfos)
    {
      return new OrganisationGroupList
      {
        OrganisationId = orgId,
        GroupList = organisationGroupInfos
      };
    }

    public static OrganisationGroupInfo GetOrganisationGroupInfo(int id, string name, DateTime createdDate)
    {
      return new OrganisationGroupInfo
      {
        GroupId = id,
        GroupName = name,
        CreatedDate = createdDate.ToString(DateTimeFormat.DateFormatShortMonth)
      };
    }

    public static OrganisationGroupRequestInfo GetOrganisationGroupRequestInfo(string groupName,
      OrganisationGroupRolePatchInfo organisationGroupRolePatchInfo, OrganisationGroupUserPatchInfo organisationGroupUserPatchInfo)
    {
      return new OrganisationGroupRequestInfo
      {
        GroupName = groupName,
        RoleInfo = organisationGroupRolePatchInfo,
        UserInfo = organisationGroupUserPatchInfo
      };
    }

    public static OrganisationGroupRolePatchInfo GetOrganisationGroupRolePatchInfo(List<int> added, List<int> removed)
    {
      return new OrganisationGroupRolePatchInfo
      {
        AddedRoleIds = added,
        RemovedRoleIds = removed
      };
    }

    public static OrganisationGroupUserPatchInfo GetOrganisationGroupUserPatchInfo(List<string> added, List<string> removed)
    {
      return new OrganisationGroupUserPatchInfo
      {
        AddedUserIds = added,
        RemovedUserIds = removed
      };
    }
  }
}
