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

  // #Auto validation
  public enum OrgAutoValidationStatus
  {
    AutoApproved,
    AutoPending,
    ManuallyApproved,
    ManuallyDecliend,
    ManualRemovalOfRightToBuy
  }
  
}
