namespace CcsSso.Domain.Constants
{
  public static class Contstant
  {
    public const string InvalidContactType = "INVALID_CONTACT_TYPE";
    public const string DAApiRequestContentType = "application/json";
    public const string ConclaveIdamConnectionName = "Username-Password-Authentication";
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
    public const string ErrorPhoneNumberRequired = "ERROR_PHONE_NUMBER_REQUIRED";
    public const string ErrorAddressRequired = "ERROR_ADDRESS_REQUIRED";
    public const string ErrorPartyIdRequired = "ERROR_PARTY_ID_REQUIRED";
    public const string ErrorOrganisationIdRequired = "ERROR_ORGANISATION_ID_REQUIRED";
    public const string ErrorInvalidIdentifier = "INVALID_IDENTIFIER";
    public const string ErrorInvalidOrganisationName = "INVALID_LEGAL_NAME";
    public const string ErrorInvalidOrganisationUri = "INVALID_URI";
    public const string ErrorInvalidSiteName = "INVALID_SITE_NAME";
    public const string ErrorInsufficientDetails = "INSUFFICIENT_DETAILS";
    public const string ErrorInvalidUserId = "INVALID_USER_ID";
    public const string ErrorInvalidFirstName = "INVALID_FIRST_NAME";
    public const string ErrorInvalidLastName = "INVALID_LAST_NAME";
    public const string ErrorInvalidUserGroup = "INVALID_USER_GROUP";
    public const string ErrorInvalidIdentityProvider = "INVALID_IDENTITY_PROVIDER";
  }

  public static class VirtualContactTypeName
  {
    public const string Name = "NAME";
    public const string Email = "EMAIL";
    public const string Phone = "PHONE";
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
}
