using CcsSso.Core.Domain.Dtos;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IAuthService
  {
    Task<bool> ValidateBackChannelLogoutTokenAsync(string backChanelLogoutToken);

    Task ChangePasswordAsync(ChangePasswordDto changePassword);

    Task SendResetMfaNotificationAsync(MfaResetInfo mfaResetInfo, bool forceUserSignout = false);

    Task ResetMfaByTicketAsync(MfaResetInfo mfaResetInfo);

    bool AuthorizeUser(string[] claimList);

    Task<bool> AuthorizeForOrganisationAsync(RequestType requestType);
  }
}
