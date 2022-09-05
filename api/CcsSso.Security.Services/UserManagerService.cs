using CcsSso.Security.Domain.Constants;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain.Helpers;
using System;
using System.Threading.Tasks;
using static CcsSso.Security.Domain.Constants.Constants;

namespace CcsSso.Security.Services
{
  public class UserManagerService : IUserManagerService
  {
    private readonly IIdentityProviderService _identityProviderService;
    private readonly ISecurityCacheService _securityCacheService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly RequestContext _requestContext;
    public UserManagerService(IIdentityProviderService identityProviderService, ISecurityCacheService securityCacheService,
      ApplicationConfigurationInfo applicationConfigurationInfo, ICcsSsoEmailService ccsSsoEmailService, RequestContext requestContext)
    {
      _identityProviderService = identityProviderService;
      _securityCacheService = securityCacheService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
      _requestContext = requestContext;
    }

    public async Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo)
    {
      userInfo.Email = userInfo.Email?.ToLower();
      ValidateUser(userInfo);
      return await _identityProviderService.CreateUserAsync(userInfo);
    }

    public async Task UpdateUserAsync(UserInfo userInfo)
    {
      ValidateUser(userInfo);
      if (string.IsNullOrWhiteSpace(userInfo.Id))
      {
        throw new CcsSsoException(Constants.ErrorCodes.UserIdRequired);
      }
      await _identityProviderService.UpdateUserAsync(userInfo);
    }

    private void ValidateUser(UserInfo userInfo)
    {
      if (string.IsNullOrWhiteSpace(userInfo.FirstName))
      {
        throw new CcsSsoException(ErrorCodes.FirstNameRequired);
      }

      if (string.IsNullOrWhiteSpace(userInfo.LastName))
      {
        throw new CcsSsoException(ErrorCodes.LastNameRequired);
      }

      if (string.IsNullOrWhiteSpace(userInfo.Email))
      {
        throw new CcsSsoException(ErrorCodes.EmailRequired);
      }

      if (!UtilityHelper.IsEmailFormatValid(userInfo.Email))
      {
        throw new CcsSsoException(ErrorCodes.EmailFormatError);
      }

      if (!UtilityHelper.IsEmailLengthValid(userInfo.Email))
      {
        throw new CcsSsoException(ErrorCodes.EmailTooLongError);
      }
    }

    public async Task DeleteUserAsync(string email)
    {
      await _identityProviderService.DeleteAsync(email);
    }

    public async Task UpdateUserMfaFlagAsync(UserInfo userInfo)
    {
      await _identityProviderService.UpdateUserMfaFlagAsync(userInfo);
    }

    public async Task ResetMfaAsync(string ticket, string userName)
    {
      var cacheKey = Constants.CacheKey.MFA_RESET + ticket;
      if (!string.IsNullOrEmpty(ticket))
      {
        userName = await _securityCacheService.GetValueAsync<string>(cacheKey);
        if (string.IsNullOrEmpty(userName))
        {
          userName = await _securityCacheService.GetValueAsync<string>(CacheKey.MFA_RESET_PERSISTENT + ticket);
          throw new SecurityException(new ErrorInfo()
          {
            Error = ErrorCodes.InvalidTicket,
            ErrorDescription = userName ?? string.Empty
          });
        }
      }

      if (!string.IsNullOrEmpty(userName))
      {
        await _identityProviderService.ResetMfaAsync(userName);
        await _securityCacheService.RemoveAsync(cacheKey);
        await _identityProviderService.UpdatePendingMFAVerifiedFlagAsync(userName, true);
      }
      else
      {
        throw new CcsSsoException(Constants.ErrorCodes.UserIdRequired);
      }
    }

    public async Task SendResetMfaNotificationAsync(MfaResetRequest mfaResetRequest)
    {
      if (string.IsNullOrEmpty(mfaResetRequest.UserName))
      {
        throw new CcsSsoException(Constants.ErrorCodes.UserIdRequired);
      }

      ValidateEmail(mfaResetRequest.UserName);

      var ticket = Guid.NewGuid().ToString().Replace("-", string.Empty);
      var cachedTicket = await _securityCacheService.GetValueAsync<string>(Constants.CacheKey.MFA_RESET + mfaResetRequest.UserName);
      if (!string.IsNullOrEmpty(cachedTicket))
      {
        await _securityCacheService.RemoveAsync(CacheKey.MFA_RESET + ticket);
      }

      await _securityCacheService.SetValueAsync(CacheKey.MFA_RESET + mfaResetRequest.UserName, ticket,
        new TimeSpan(0, _applicationConfigurationInfo.MfaSetting.TicketExpirationInMinutes, 0));

      await _securityCacheService.SetValueAsync(CacheKey.MFA_RESET + ticket, mfaResetRequest.UserName, new TimeSpan(0, _applicationConfigurationInfo.MfaSetting.TicketExpirationInMinutes, 0));
      var url = $"{_applicationConfigurationInfo.MfaSetting.MfaResetRedirectUri}?t=" + ticket;
      await _ccsSsoEmailService.SendResetMfaEmailAsync(mfaResetRequest.UserName, url);
      //Defaut 1 month -> Remeber users every MFA reset ticket
      await _securityCacheService.SetValueAsync(CacheKey.MFA_RESET_PERSISTENT + ticket, mfaResetRequest.UserName, new TimeSpan(_applicationConfigurationInfo.MfaSetting.MFAResetPersistentTicketListExpirationInDays, 0, 0, 0, 0));
      if (mfaResetRequest.ForceUserSignout)
      {
        await _identityProviderService.UpdatePendingMFAVerifiedFlagAsync(mfaResetRequest.UserName, false);
      }
    }

    public async Task<IdamUserInfo> GetUserAsync()
    {
      Console.WriteLine($"User Name Inside the Token:- {_requestContext.UserName}");
      return await _identityProviderService.GetIdamUserInfoAsync(_requestContext.UserName);
    }

    public async Task<IdamUser> GetUserAsync(string email)
    {
      ValidateEmail(email);

      return await _identityProviderService.GetIdamUserByEmailAsync(email);
    }

    public async Task SendUserActivationEmailAsync(string email, bool isExpired = false)
    {
      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailRequired);
      }
      await _identityProviderService.SendUserActivationEmailAsync(email.ToLower(), null, isExpired);
    }
    public async Task<string> GetActivationEmailVerificationLink(string email)
    {
      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailRequired);
      }
      return await _identityProviderService.GetActivationEmailVerificationLink(email.ToLower());
    }

    private void ValidateEmail(string email)
    {
      if (!UtilityHelper.IsEmailFormatValid(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailFormatError);
      }

      if (!UtilityHelper.IsEmailLengthValid(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailTooLongError);
      }
    }
  }
}
