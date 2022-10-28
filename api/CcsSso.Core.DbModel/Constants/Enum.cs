namespace CcsSso.Core.DbModel.Constants
{
  public enum RoleEligibleOrgType
  {
    Internal,
    External,
    Both
  }

  public enum RoleEligibleSubscriptionType
  {
    Default,
    Subscription
  }

  public enum RoleEligibleTradeType
  {
    Supplier,
    Buyer,
    Both
  }

  public enum AssignedContactType
  {
    None,
    User,
    Site
  }

  public enum BulkUploadStatus
  {
    Processing,
    DocUploadValidationFail,
    Validating,
    ValidationFail,
    Migrating,
    MigrationCompleted
  }

  public enum UserType
  {
    Primary,
    Delegation
  }

  public enum OrganisationAuditActionType
  {
    Autovalidation,
    OrganisationRegistration,
    Admin
  }

  public enum OrganisationAuditEventType
  {
    RoleAssigned,
    RoleUnassigned,
    ManualAcceptationRightToBuy,
    ManualDeclineRightToBuy,
    AutomaticAcceptationRightToBuy,
    AutomaticDeclineRightToBuy,
    OrganisationTypeBuyerToSupplier,
    OrganisationTypeBuyerToBoth,
    OrganisationTypeBothToSupplier,
    OrganisationTypeBothToBuyer,
    OrganisationTypeSupplierToBoth,
    OrganisationTypeSupplierToBuyer,
    NotRecognizedAsVerifiedBuyer,
    OrganisationRegistrationTypeBuyer,
    OrganisationRegistrationTypeBoth,
    InactiveOrganisationRemoved
  }
}
