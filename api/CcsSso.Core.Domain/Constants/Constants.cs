namespace CcsSso.Domain.Constants
{
  public static class Contstant
  {
    public const string InvalidContactType = "INVALID_CONTACT_TYPE";
    public const string DAApiRequestContentType = "application/json";
    public const string ConclaveIdamConnectionName = "Username-Password-Authentication";
    public const string OrgAdminRoleNameKey = "ORG_ADMINISTRATOR";
    public const string DefaultUserRoleNameKey = "ORG_DEFAULT_USER";
    public const string DefaultAdminUserGroupName = "Organisation Administrators";
    public const string DefaultAdminUserGroupNameKey = "ORGANISATION_ADMINISTRATORS";
    public const string FleetPortalUserRoleNameKey = "FP_USER";
  }

  public enum WrapperApi
  {
    Configuration,
    Organisation,
    OrganisationDelete,
    Contact,
    ContactDelete,
    Security,
    User,
    Cii
  }
}
