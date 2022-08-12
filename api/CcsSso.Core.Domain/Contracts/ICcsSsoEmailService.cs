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

        Task SendUserConfirmEmailOnlyUserIdPwdAsync(string email, string activationlink);
        Task SendUserConfirmEmailOnlyFederatedIdpAsync(string email, string idpName);
        Task SendUserConfirmEmailBothIdpAsync(string email, string idpName, string activationlink);
        Task SendUserRegistrationEmailUserIdPwdAsync(string email, string activationlink);


        Task SendOrgProfileUpdateEmailAsync(string email);

        Task SendNominateEmailAsync(string email, string link);

        Task SendUserProfileUpdateEmailAsync(string email);

        Task SendUserContactUpdateEmailAsync(string email);

        Task SendUserPermissionUpdateEmailAsync(string email);

        Task SendOrgJoinRequestEmailAsync(OrgJoinNotificationInfo orgJoinNotificationInfo);

        Task SendUserDelegatedAccessEmailAsync(string email, string orgName, string encryptedInfo);
    }
}
