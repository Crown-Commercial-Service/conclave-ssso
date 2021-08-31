namespace CcsSso.Shared.Domain.Constants
{
  public static class CacheKeyConstant
  {
    public const string User = "WRAPPER_USER"; // WRAPPER_USER-{userName}
    public const string Organisation = "WRAPPER_ORG"; // WRAPPER_ORG-{ciiOrganisationId}
    public const string OrganisationUsers = "WRAPPER_ORG_USERS"; // WRAPPER_ORG_USERS-{ciiOrganisationId}
    public const string OrgSites = "WRAPPER_ORG_SITES"; // WRAPPER_ORG_SITES-{ciiOrganisationId}
    public const string Site = "WRAPPER_ORG_SITE"; // WRAPPER_ORG_SITE-{ciiOrganisationId}-{siteId}
    public const string UserContactPoints = "WRAPPER_USER_CONTACT_POINTS"; // WRAPPER_USER_CONTACT_POINTS-{userName}
    public const string OrganisationContactPoints = "WRAPPER_ORG_CONTACT_POINTS"; // WRAPPER_ORG_CONTACT_POINTS-{ciiOrganisationId}
    public const string SiteContactPoints = "WRAPPER_ORG_SITE_CONTACT_POINTS"; // WRAPPER_ORG_SITE_CONTACT_POINTS-{ciiOrganisationId}-{siteId}
    public const string Contact = "WRAPPER_CONTACT"; // WRAPPER_CONTACT-{contactId}
    public const string UserContactPoint = "WRAPPER_USER_CONTACT_POINT"; // WRAPPER_USER_CONTACT_POINT-{userName}-{contactPointId}
    public const string OrganisationContactPoint = "WRAPPER_ORG_CONTACT_POINT"; // WRAPPER_ORG_CONTACT_POINT-{ciiOrganisationId}-{contactPointId}
    public const string SiteContactPoint = "WRAPPER_ORG_SITE_CONTACT_POINT"; // WRAPPER_ORG_SITE_CONTACT_POINT-{ciiOrganisationId}-{siteId}-{contactPointId}
    public const string ForceSignoutKey = "FORCE_SIGNOUT-";
    public const string BlockedListKey = "BLOCKED-LIST";
    public const string UserOrganisation = "USER_ORGANISATION"; // USER_ORGANISATION-{userName}
  }
}
