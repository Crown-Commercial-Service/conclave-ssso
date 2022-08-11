using CcsSso.Core.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
    public interface ICcsSsoEmailService
    {
        Task SendUserWelcomeEmailAsync(string email, string idpName);

        Task SendOrgProfileUpdateEmailAsync(string email);

        Task SendNominateEmailAsync(string email, string link);

        Task SendUserProfileUpdateEmailAsync(string email);

        Task SendUserContactUpdateEmailAsync(string email);

        Task SendUserPermissionUpdateEmailAsync(string email);

        Task SendOrgJoinRequestEmailAsync(OrgJoinNotificationInfo orgJoinNotificationInfo);

        Task SendUserDelegatedAccessEmailAsync(string email, string orgName, string encryptedInfo);
    }
}
