namespace CcsSso.Adaptor.Domain.Constants
{
  public static class ConclaveEntityNames
  {
    public const string UserProfile = "UserProfile";
    public const string OrgProfile = "OrgProfile";
    public const string Site = "SiteProfile";
    public const string UserContact = "UserContact";
    public const string OrgContact = "OrgContact";
    public const string SiteContact = "SiteContact";
    public const string Contact = "Contact";
    public const string OrgUser = "OrgUser";
    public const string OrgIdentifiers = "CiiOrgIdentifiers";
  }

  public static class ConsumerEntityNames
  {
    public const string User = "User";
    public const string Organisation = "Organisation";
    public const string Contact = "Contact";
    public const string UserContact = "UserContact";
    public const string OrganisationContact = "OrgContact";
    public const string SiteContact = "SiteContact";
  }

  public static class ConclaveAttributeNames
  {
    public const string ContactObject = "Contact";
  }

  public static class ErrorConstant
  {
    public const string NoConfigurationFound = "ERROR_NO_CONFIGURATION_FOUND";
    public const string NoSubscriptionFound = "ERROR_NO_SUBSCRIPTION_AVAILABLE";
  }

  public static class ContactType
  {
    public const string Email = "EMAIL";
    public const string Phone = "PHONE";
    public const string Mobile = "MOBILE";
    public const string Fax = "FAX";
    public const string Url = "WEB_ADDRESS";
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
}
