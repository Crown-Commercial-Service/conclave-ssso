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
    Admin,
    Job
  }

  public enum OrganisationAuditEventType
  {
    OrgRoleAssigned,
    OrgRoleUnassigned,
    AdminRoleAssigned,
    AdminRoleUnassigned,
    ManualAcceptationRightToBuy,
    ManualDeclineRightToBuy,
    AutomaticAcceptationRightToBuy,
    //AutomaticDeclineRightToBuy,
    OrganisationTypeBuyerToSupplier,
    OrganisationTypeBuyerToBoth,
    OrganisationTypeBothToSupplier,
    OrganisationTypeBothToBuyer,
    OrganisationTypeSupplierToBoth,
    OrganisationTypeSupplierToBuyer,
    NotRecognizedAsVerifiedBuyer,
    OrganisationRegistrationTypeBuyer,
    OrganisationRegistrationTypeBoth,
    InactiveOrganisationRemoved,
    ManualRemoveRightToBuy
  }

  // #Auto validation
  public enum OrgAutoValidationStatus
  {
    AutoApproved,
    AutoPending,
    ManuallyApproved,
    ManuallyDecliend,
    ManualRemovalOfRightToBuy,
    AutoOrgRemoval,
    ManualPending,
  }

  public enum ManualValidateOrganisationStatus
  {
    Approve,
    Decline,
    Remove
  }

  public enum RoleApprovalRequiredStatus
  {
    ApprovalNotRequired,
    ApprovalRequired
  }

  public enum UserPendingRoleStaus
  {
    Pending,
    Approved,
    Rejected,
    Removed,
    Expired
  }
}
