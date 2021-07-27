using CcsSso.Security.Domain.Constants;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
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
    public UserManagerService(IIdentityProviderService identityProviderService, ISecurityCacheService securityCacheService,
      ApplicationConfigurationInfo applicationConfigurationInfo, ICcsSsoEmailService ccsSsoEmailService)
    {
      _identityProviderService = identityProviderService;
      _securityCacheService = securityCacheService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
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
        throw new CcsSsoException(Constants.ErrorCodes.FirstNameRequired);
      }

      if (string.IsNullOrWhiteSpace(userInfo.LastName))
      {
        throw new CcsSsoException(Constants.ErrorCodes.LastNameRequired);
      }

      if (string.IsNullOrWhiteSpace(userInfo.Email))
      {
        throw new CcsSsoException(Constants.ErrorCodes.EmailRequired);
      }

      if (!UtilitiesHelper.IsEmailValid(userInfo.Email))
      {
        throw new CcsSsoException(Constants.ErrorCodes.EmailFormatError);
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
          throw new CcsSsoException(Constants.ErrorCodes.InvalidTicket);
        }
      }

      if (!string.IsNullOrEmpty(userName))
      {
        await _identityProviderService.ResetMfaAsync(userName);
        await _securityCacheService.RemoveAsync(cacheKey);
      }
      else
      {
        throw new CcsSsoException(Constants.ErrorCodes.UserIdRequired);
      }
    }

    public async Task SendResetMfaNotificationAsync(string userName)
    {
      var ticket = Guid.NewGuid().ToString().Replace("-", string.Empty);
      var cachedTicket = await _securityCacheService.GetValueAsync<string>(Constants.CacheKey.MFA_RESET + userName);
      if (!string.IsNullOrEmpty(cachedTicket))
      {
        await _securityCacheService.RemoveAsync(Constants.CacheKey.MFA_RESET + ticket);
      }

      await _securityCacheService.SetValueAsync(Constants.CacheKey.MFA_RESET + userName, ticket,
        new TimeSpan(0, _applicationConfigurationInfo.MfaSetting.TicketExpirationInMinutes, 0));

      await _securityCacheService.SetValueAsync(Constants.CacheKey.MFA_RESET + ticket, userName, new TimeSpan(0, _applicationConfigurationInfo.MfaSetting.TicketExpirationInMinutes, 0));
      var url = $"{_applicationConfigurationInfo.MfaSetting.MfaResetRedirectUri}?t=" + ticket;
      await _ccsSsoEmailService.SendResetMfaEmailAsync(userName, url);
    }

    public async Task NominateUserAsync(UserInfo userInfo)
    {
      userInfo.Email = userInfo.Email.ToLower();
      await _identityProviderService.SendNominateEmailAsync(userInfo);
    }

    public async Task<IdamUser> GetUserAsync(string email)
    {
      return await _identityProviderService.GetUser(email);
    }

    public async Task SendUserActivationEmailAsync(string email)
    {
      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailRequired);
      }
      await _identityProviderService.SendUserActivationEmailAsync(email.ToLower());
    }
  }
}
