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

  public static class Constants
  {
    public const int EmailMaxCharacters = 256;
    public const int EmailUserNameMaxCharacters = 64;
    public const int EmaildomainnameNameMaxCharacters = 63;
    public const int EmaildomainMaxCharacters = 63;
  }

  public static class UserHeaderMap
  {
    public const string ID = "id";
    public const string UserName = "username";
    public const string OrganisationID = "organisation_id";
    public const string FirstName = "firstname";
    public const string LastName = "lastname";
    public const string Title = "title";
    public const string mfaEnabled = "mfa_enabled";
    public const string AccountVerified = "accountverified";
    public const string SendUserRegistrationEmail = "send_user_registrationemail";
    public const string IsAdminUser = "is_adminuser";
    public const string UserGroups = "usergroups";
    public const string RolePermissionInfo = "rolepermissioninfo";
    public const string IdentityProviders = "identityproviders";

  }

  public static class OrganisationHeaderMap
  {
    public const string Identifier_Id = "identifier_id";
    public const string Identifier_LegalName = "identifier_legalname";
    public const string Identifier_Uri = "identifier_uri";
    public const string Identifier_Scheme = "identifier_scheme";
    public const string AdditionalIdentifiers = "additionalIdentifiers";
    public const string Address_StreetAddress = "address_streetaddress";
    public const string Address_Locality = "address_locality";
    public const string Address_Region = "address_region";
    public const string Address_PostalCode = "address_postalcode";
    public const string Address_CountryCode = "address_countrycode";
    public const string Address_CountryName = "address_countryname";
    public const string Detail_Organisation_Id = "detail_organisation_id";
    public const string Detail_CreationDate = "detail_creationdate";
    public const string Detail_BusinessType = "detail_businesstype";
    public const string Detail_SupplierBuyerType = "detail_supplierbuyertype";
    public const string Detail_IsSme = "detail_is_sme";
    public const string Detail_IsVcse = "detail_is_vcse";
    public const string Detail_RightToBuy = "detail_rightTobuy";
    public const string Detail_IsActive = "detail_isactive";

    public const string AdditionalIdentifiers_Id = "Id";
    public const string AdditionalIdentifiers_LegalName = "LegalName";
    public const string AdditionalIdentifiers_URI = "Uri";
    public const string AdditionalIdentifiers_Scheme = "Scheme";

    public const string AdditionalIdentifiers_NA = "NA";

  }

  public static class ContactsHeaderMap
  {
    //public const string ContactType = "contact_type";
    //public const string ContactID = "contact_id";
    public const string ContactType = "type";
    public const string ContactID = "type_id";
    public const string ContactPointID = "contactpoint_id";
    public const string OriginalContactPointID = "original_contactpoint_id";
    public const string AssignedContactType = "assigned_contact_type";
    public const string Contact_ContactID = "contacts_contactid";
    public const string Contacts_ContactType = "contacts_contacttype";
    public const string Contacts_ContactValue = "contacts_contactvalue";
    public const string ContactPoint_Reason = "contactpoint_reason";
    public const string ContactPoint_Name = "contactpoint_name";
  }

  public static class AuditLogHeaderMap
  {
    public const string ID = "ID";
    public const string Event = "Event";
    public const string UserId = "UserId";
    public const string Application = "Application";
    public const string ReferenceData = "ReferenceData";
    public const string IpAddress = "IpAddress";
    public const string Device = "Device";
    public const string EventTimeUtc = "EventTimeUtc";
  }
}
