using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CcsSso.Security.Domain.Dtos
{
  public class UserInfo
  {
    public string Id { get; set; }

    public string UserName { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }

    public bool SendUserRegistrationEmail { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Role { get; set; }

    public bool MfaEnabled { get; set; }

    public List<string> Groups { get; set; }

    public string ProfilePageUrl { get; set; }
  }

  public class IdamUser
  {
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public bool EmailVerified { get; set; }

    public int LoginCount { get; set; }
  }

  public class IdamUserInfo
  {
    [JsonPropertyName("sub")]
    public string Sub { get; set; }

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; }

    [JsonPropertyName("nick_name")]
    public string NickName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("picture")]
    public string Picture { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }
  }


  public class UserProfileInfo
  {
    public UserDetail Detail { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public bool AccountVerified { get; set; }

    public string OrganisationId { get; set; }
  }

  public class UserDetail
  {
    public int Id { get; set; }

    public string IdentityProvider { get; set; }

    public List<GroupAccessRole> UserGroups { get; set; }

    public List<RolePermissionInfo> RolePermissionInfo { get; set; }

    public List<UserIdentityProviderInfo> IdentityProviders { get; set; }
  }

  public class UserIdentityProviderInfo
  {
    public int IdentityProviderId { get; set; }

    public string IdentityProvider { get; set; }
  }

  public class RolePermissionInfo
  {
    public string RoleKey { get; set; }

    public string ServiceClientId { get; set; }
  }

  public class GroupAccessRole
  {
    public string AccessRole { get; set; }

    public string ServiceClientId { get; set; }
  }
}
