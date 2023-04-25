namespace CcsSso.Domain.Constants
{
  public static class Contstant
  {
    public const string InvalidContactType = "INVALID_CONTACT_TYPE";
    public const string DAApiRequestContentType = "application/json";
    public const string ConclaveIdamConnectionName = "Username-Password-Authentication";
    public const string OrgAdminRoleNameKey = "ORG_ADMINISTRATOR";
    public const string DefaultUserRoleNameKey = "ORG_DEFAULT_USER";
  }

  public static class ErrorConstant
  {
    public const string EntityNotFound = "ERROR_ENTITY_NOT_FOUND";
    public const string ErrorInvalidContacts = "INVALID_CONTACTS";
    public const string ErrorInvalidContactReason = "INVALID_CONTACT_REASON";
    public const string ErrorNameRequired = "ERROR_NAME_REQUIRED";
    public const string ErrorEmailRequired = "ERROR_EMAIL_REQUIRED";
    public const string ErrorInvalidEmail = "INVALID_EMAIL";
    public const string ErrorInvalidPhoneNumber = "INVALID_PHONE_NUMBER";
    public const string ErrorInvalidMobileNumber = "INVALID_MOBILE_NUMBER";
    public const string ErrorInvalidFaxNumber = "INVALID_FAX_NUMBER";
    public const string ErrorPhoneNumberRequired = "ERROR_PHONE_NUMBER_REQUIRED";
    public const string ErrorAddressRequired = "ERROR_ADDRESS_REQUIRED";
    public const string ErrorPartyIdRequired = "ERROR_PARTY_ID_REQUIRED";
    public const string ErrorOrganisationIdRequired = "ERROR_ORGANISATION_ID_REQUIRED";
    public const string ErrorInvalidIdentifier = "INVALID_IDENTIFIER";
    public const string ErrorInvalidDetails = "INVALID_DETAILS";
    public const string ErrorInvalidCiiOrganisationId = "INVALID_CII_ORGANISATION_ID";
    public const string ErrorInvalidOrganisationName = "INVALID_LEGAL_NAME";
    public const string ErrorInvalidOrganisationUri = "INVALID_URI";
    public const string ErrorInvalidSiteName = "INVALID_SITE_NAME";
    public const string ErrorInvalidSiteAddress = "INVALID_SITE_ADDRESS";
    public const string ErrorInvalidStreetAddress = "INVALID_STREET_ADDRESS";
    public const string ErrorInvalidlocality = "INVALID_LOCALITY";
    public const string ErrorCountyRequired = "ERROR_COUNTRY_REQUIRED";
    public const string ErrorContactNameRequired = "ERROR_CONTACT_POINT_NAME_REQUIRED";
    public const string ErrorInvalidContactPointName = "ERROR_INVALID_CONTACT_POINT_NAME";
    public const string ErrorInsufficientDetails = "INSUFFICIENT_DETAILS";
    public const string ErrorContactsRequired = "ERROR_CONTACTS_REQUIRED";
    public const string ErrorInvalidUserId = "INVALID_USER_ID";
    public const string ErrorUserIdTooLong = "ERROR_USER_ID_TOO_LONG";
    public const string ErrorEmailTooLong = "ERROR_EMAIL_TOO_LONG";
    public const string ErrorInvalidFirstName = "INVALID_FIRST_NAME";
    public const string ErrorInvalidLastName = "INVALID_LAST_NAME";
    public const string ErrorInvalidFirstNamelength = "ERROR_FIRST_NAME_TOO_SHORT";
    public const string ErrorInvalidLastNamelength = "ERROR_LAST_NAME_TOO_SHORT";
    public const string ErrorInvalidUserDetail = "INVALID_USER_DETAIL";
    public const string ErrorInvalidTitle = "INVALID_TITLE";
    public const string ErrorInvalidUserGroupRole = "INVALID_USER_GROUP_ROLE";
    public const string ErrorInvalidUserGroup = "INVALID_USER_GROUP";
    public const string ErrorInvalidUserRole = "INVALID_ROLE";
    public const string ErrorInvalidIdentityProvider = "INVALID_IDENTITY_PROVIDER";
    public const string ErrorCannotDeleteLastOrgAdmin = "ERROR_CANNOT_DELETE_LAST_ADMIN_OF_ORGANISATION";
    public const string ErrorCannotRemoveAdminRoleGroupLastOrgAdmin = "ERROR_CANNOT_REMOVE_ADMIN_ROLE_OR_GROUP_OF_LAST_ADMIN";
    public const string ErrorInvalidGroupName = "INVALID_GROUP_NAME";
    public const string ErrorInvalidRoleInfo = "INVALID_ROLE_INFO";
    public const string ErrorInvalidUserInfo = "INVALID_USER_INFO";
    public const string ErrorInvalidContactType = "INVALID_CONTACT_TYPE";
    public const string ErrorInvalidContactValue = "INVALID_CONTACT_VALUE";
    public const string ErrorInvalidCountryCode = "INVALID_COUNTRY_CODE";
    public const string ErrorInvalidAssigningContactIds = "ERROR_INVALID_ASSIGNING_CONTACT_POINT_IDS";
    public const string ErrorInvalidUnassigningContactIds = "ERROR_INVALID_UNASSIGNING_CONTACT_POINT_IDS";
    public const string ErrorInvalidUserIdForContactAssignment = "ERROR_INVALID_USER_ID_FOR_CONTACT_ASSIGNEMNT";
    public const string ErrorInvalidSiteIdForContactAssignment = "ERROR_INVALID_SITE_ID_FOR_CONTACT_ASSIGNEMNT";
    public const string ErrorInvalidContactAssignmentType = "ERROR_INVALID_CONTACT_ASSIGNEMNT_TYPE";
    public const string ErrorDuplicateContactAssignment = "ERROR_DUPLICATE_CONTACT_ASSIGNMENT";
    public const string ErrorMfaFlagRequired = "MFA_DISABLED_USER";
    public const string ErrorMfaFlagForInvalidConnection = "MFA_ENABLED_INVALID_CONNECTION";
    public const string ErrorInvalidStatusInfo = "INVALID_STATUS_INFO";
    public const string ErrorInvalidService = "INVALID_SERVICE";

    // #Delegated
    public const string ErrorInvalidUserDelegationPrimaryDetails = "INVALID_USER_DELEGATION_PRIMARY_DETAILS";
    public const string ErrorInvalidUserDelegation = "INVALID_USER_DELEGATION";
    public const string ErrorInvalidUserDelegationSameOrg = "INVALID_USER_DELEGATION_SAME_ORG";
    public const string ErrorActivationLinkExpired = "ERROR_ACTIVATION_LINK_EXPIRED";
    public const string ErrorSendingActivationLink = "ERROR_SENDING_ACTIVATION_LINK";

    public const string ErrorUserHasValidDomain = "USER_HAS_VALID_DOMAIN";
    public const string ErrorInvalidCiiOrganisationIdOrUserId = "INVALID_CII_ORGANISATION_ID_OR_USER_ID";

    public const string ErrorLinkExpired = "ERROR_LINK_EXPIRED";
    public const string ErrorUserAlreadyExists = "ERROR_USER_ALREADY_EXISTS";

  }

  public static class VirtualContactTypeName
  {
    public const string Name = "NAME";
    public const string Email = "EMAIL";
    public const string Phone = "PHONE";
    public const string Mobile = "MOBILE";
    public const string Fax = "FAX";
    public const string Url = "WEB_ADDRESS";
  }

  public static class PartyTypeName
  {
    public const string User = "USER";
    public const string NonUser = "NON_USER";
    public const string InternalOrgnaisation = "INTERNAL_ORGANISATION";
    public const string ExternalOrgnaisation = "EXTERNAL_ORGANISATION";
  }

  public static class DateTimeFormat
  {
    public const string DateFormat = "dd/MM/yyyy";
    public const string DateFormatShortMonth = "dd MMM yyyy";
  }

  public static class ContactReasonType
  {
    public const string Other = "OTHER";
    public const string Billing = "BILLING";
    public const string Shipping = "SHIPPING";
    public const string MainOffice = "MAIN_OFFICE";
    public const string HeadQuarters = "HEAD_QUARTERS";
    public const string Manufacturing = "MANUFACTURING";
    public const string Branch = "BRANCH";
    public const string Site = "SITE";
    public const string Unspecified = "UNSPECIFIED";
  }

  public static class ConclaveEntityNames
  {
    public const string UserProfile = "UserProfile";
    public const string OrgProfile = "OrgProfile";
    public const string Site = "SiteProfile";
    public const string UserContact = "UserContact";
    public const string OrgContact = "OrgContact";
    public const string SiteContact = "SiteContact";
    public const string Contact = "Contact";
  }

  public static class QueueConstant
  {
    public const string OperationName = "OperationName";
    public const string OperationEntity = "OperationEntity";
    public const string OrganisationIdAttribute = "OrganisationId";
    public const string UserNameAttribute = "UserName";
    public const string ContactIdAttribute = "ContactId";
    public const string SiteIdAttribute = "SiteId";
  }

  public static class OperationType
  {
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
  }

  public static class CacheKeys
  {
    public const string CcsServices = "LOCAL_CACHE_CCS_SERVICES";
    public const string DashboardServiceId = "DASHBOARD_SERVICE_ID";
  }

  public static class AuditLogEvent
  {
    public const string UserCreate = "User-create";
    public const string UserDelete = "User-delete";
    public const string UserUpdate = "User-update";
    // #Delegated
    public const string UserDelegated = "User-delegated";
    public const string UserIdpUpdate = "User-idp-update";
    public const string UserGroupUpdate = "User-group-update";
    public const string UserRoleUpdate = "User-role-update";
    public const string MyAccountUpdate = "My-account-update";
    public const string UserContactCreate = "User-contact-create";
    public const string UserContactDelete = "User-contact-delete";
    public const string UserContactUpdate = "User-contact-update";
    public const string OrgContactCreate = "Org-contact-create";
    public const string OrgContactDelete = "Org-contact-delete";
    public const string OrgContactUpdate = "Org-contact-update";
    public const string OrgSiteCreate = "Org-site-create";
    public const string OrgSiteDelete = "Org-site-delete";
    public const string OrgSiteUpdate = "Org-site-update";
    public const string GroupeCreate = "Group-create";
    public const string GroupeDelete = "Group-delete";
    public const string GroupeNameChange = "Group-name-changed";
    public const string GroupeRoleAdd = "Group-role-add";
    public const string GroupeRoleRemove = "Group-role-remove";
    public const string GroupeUserAdd = "Group-user-add";
    public const string GroupeUserRemove = "Group-user-remove";
    public const string OrgRegistryAdd = "Org-registry-add";
    public const string OrgRegistryRemove = "Org-registry-remove";
    public const string UserPasswordChange = "User-password-change";
    public const string AdminResetPassword = "Admin-reset-user-password";
    public const string RemoveUserAdminRoles = "Admin-remove-user-admin-roles";
    public const string AddUserAdminRole = "Admin-add-user-admin-role";
  }

  public static class AuditLogApplication
  {
    public const string ManageMyAccount = "Manage-my-account";
    public const string ManageUserAccount = "Manage-user-account";
    public const string ManageOrganisation = "Manage-organisation";
    public const string ManageGroup = "Manage-group";
    public const string OrgUserSupport = "Org-user-support";
  }
}
