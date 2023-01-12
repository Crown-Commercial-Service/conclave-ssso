using CcsSso.Core.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface ICcsSsoEmailService
  {
    Task SendUserWelcomeEmailAsync(string email, string idpName);

    Task SendUserUpdateEmailOnlyUserIdPwdAsync(string email, string activationlink);
    Task SendUserUpdateEmailOnlyFederatedIdpAsync(string email, string idpName);
    Task SendUserUpdateEmailBothIdpAsync(string email, string idpName, string activationlink);

    // #Auto validation
    Task SendUserConfirmEmailOnlyUserIdPwdAsync(string email, string activationlink, string ccsMsg);
    Task SendUserConfirmEmailOnlyFederatedIdpAsync(string email, string idpName);
    Task SendUserConfirmEmailBothIdpAsync(string email, string idpName, string activationlink);
    Task SendUserRegistrationEmailUserIdPwdAsync(string email, string activationlink);


    Task SendOrgProfileUpdateEmailAsync(string email);

    Task SendNominateEmailAsync(string email, string link);

    Task SendUserProfileUpdateEmailAsync(string email);

    Task SendUserContactUpdateEmailAsync(string email);

    Task SendUserPermissionUpdateEmailAsync(string email);

    Task SendUserRoleApprovalEmailAsync(string email, string userName, string orgName, string serviceName, string encryptedCode);

    Task SendOrgJoinRequestEmailAsync(OrgJoinNotificationInfo orgJoinNotificationInfo);
    // #Delegated
    Task SendUserDelegatedAccessEmailAsync(string email, string orgName, string encryptedInfo);
    // #Auto validation
    Task SendOrgPendingVerificationEmailToCCSAdminAsync(string email, string orgName);

    Task SendOrgBuyerStatusChangeUpdateToAllAdminsAsync(string email);
    Task SendOrgApproveRightToBuyStatusToAllAdminsAsync(string email);
    Task SendOrgDeclineRightToBuyStatusToAllAdminsAsync(string email);
    Task SendOrgRemoveRightToBuyStatusToAllAdminsAsync(string email);

    Task SendRoleApprovedEmailAsync(string email, string userName, string serviceName, string link);
    Task SendRoleRejectedEmailAsync(string email, string userName, string serviceName);
  }
}
