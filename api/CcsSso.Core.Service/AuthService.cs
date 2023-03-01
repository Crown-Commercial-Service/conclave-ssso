using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class AuthService : IAuthService
  {
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuditLoginService _auditLoginService;
    private readonly RequestContext _requestContext;
    private readonly IDataContext _dataContext;
    private readonly IRemoteCacheService _remoteCacheService;

    public AuthService(ApplicationConfigurationInfo applicationConfigurationInfo, ITokenService tokenService, IHttpClientFactory httpClientFactory,
      IAuditLoginService auditLoginService, RequestContext requestContext, IDataContext dataContext, IRemoteCacheService remoteCacheService)
    {
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _tokenService = tokenService;
      _httpClientFactory = httpClientFactory;
      _auditLoginService = auditLoginService;
      _requestContext = requestContext;
      _dataContext = dataContext;
      _remoteCacheService = remoteCacheService;
    }

    public async Task<bool> ValidateBackChannelLogoutTokenAsync(string backChanelLogoutToken)
    {
      var result = await _tokenService.ValidateTokenAsync(backChanelLogoutToken, _applicationConfigurationInfo.JwtTokenValidationInfo.JwksUrl, _applicationConfigurationInfo.JwtTokenValidationInfo.IdamClienId, _applicationConfigurationInfo.JwtTokenValidationInfo.Issuer);
      return result.IsValid;
    }

    public async Task ChangePasswordAsync(ChangePasswordDto changePassword)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      var url = "/security/users/passwords";

      Dictionary<string, string> requestData = new Dictionary<string, string>
          {
            { "userName", changePassword.UserName},
            { "newPassword", changePassword.NewPassword},
            { "oldPassword", changePassword.OldPassword},
          };

      HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var result = await client.PostAsync(url, data);
      if (result.StatusCode == HttpStatusCode.BadRequest)
      {
        var errorMessage = await result.Content.ReadAsStringAsync();
        throw new CcsSsoException(errorMessage);
      }

      await _auditLoginService.CreateLogAsync(AuditLogEvent.UserPasswordChange, AuditLogApplication.ManageMyAccount, $"UserId:{_requestContext.UserId}");
    }

    public async Task ResetMfaByTicketAsync(MfaResetInfo mfaResetInfo)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      var url = "/security/mfa-reset-tickets";

      if (!string.IsNullOrEmpty(mfaResetInfo.Ticket))
      {
        Dictionary<string, string> requestData = new Dictionary<string, string>
      {
        { "ticket", mfaResetInfo.Ticket}
      };

        HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
        { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

        var result = await client.PostAsync(url, data);
        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
          var errorMessage = await result.Content.ReadAsStringAsync();
          throw new CcsSsoException(errorMessage);
        }
      }
      else
      {
        throw new CcsSsoException("MFA_RESET_FAILED");
      }
    }

    public bool AuthorizeUser(string[] claimList)
    {
      var isAuthorized = _requestContext.Roles.Any(r => claimList.Any(c => r == c));

      // BUG 4399 - To fix unauthorized access to user, if api is not allowed to access by user
      if (_applicationConfigurationInfo.EnableUserAccessTokenFix)
      {
        if (isAuthorized)
        {
          isAuthorized = IsUserAuthorize(isAuthorized);
        }
      }
      else
      {
        isAuthorized = IsUserAuthorize(isAuthorized);
      }      

      if (!isAuthorized)
      {
        throw new ForbiddenException();
      }

      return isAuthorized;
    }

    private bool IsUserAuthorize(bool isAuthorized)
    {
      // Org users (having only the ORG_DEFAULT_USER role) should not access other user details
      if (_requestContext.RequestType == RequestType.NotHavingOrgId && _requestContext.Roles.Count == 1 && _requestContext.Roles.Contains("ORG_DEFAULT_USER"))
      {
        isAuthorized = _requestContext.UserName == _requestContext.RequestIntendedUserName;
      }

      return isAuthorized;
    }

    public async Task<bool> AuthorizeForOrganisationAsync(RequestType requestType)
    {
      var isOrgAdmin = _requestContext.Roles.Contains("ORG_ADMINISTRATOR");

      // #Delegated: Change to handle request for delegate user
      var isDelegateUserRequest = _requestContext.Roles.Contains("DELEGATED_USER");
      if (isDelegateUserRequest)
      {
        return true;
      }

      var isCcsAdminRequest = _requestContext.Roles.Contains("ORG_USER_SUPPORT") || _requestContext.Roles.Contains("MANAGE_SUBSCRIPTIONS");

      // #Delegated: Change to allow org admin to serach for user of other org
      var isDelegatedSearchRequest = isOrgAdmin && _requestContext.IsDelegated;

      if (isDelegatedSearchRequest && _requestContext.RequestIntendedOrganisationId != null)
      {
        if (_requestContext.CiiOrganisationId == _requestContext.RequestIntendedOrganisationId)
        {
          return true;
        }
        else if (_requestContext.CiiOrganisationId != _requestContext.RequestIntendedOrganisationId && !isCcsAdminRequest)
        {
          throw new ForbiddenException();
        }
      }

      var isAuthorizedForOrganisation = false;

      if (requestType == RequestType.HavingOrgId)
      {
        isAuthorizedForOrganisation = _requestContext.CiiOrganisationId == _requestContext.RequestIntendedOrganisationId;
      }
      else if (requestType == RequestType.NotHavingOrgId)
      {
        var intendedOrganisationId = await _remoteCacheService.GetValueAsync<string>($"{CacheKeyConstant.UserOrganisation}-{_requestContext.RequestIntendedUserName}");

        if (string.IsNullOrEmpty(intendedOrganisationId))
        {
          // Based on the delegation user logic we only come to this point when the request comes for primary user.
          // primary condition has been added to fix the issue https://crowncommercialservice.atlassian.net/jira/software/c/projects/CON/issues/CON-3108

          intendedOrganisationId = await _dataContext.User
            .Where(u => !u.IsDeleted && u.UserName == _requestContext.RequestIntendedUserName && (u.UserType == UserType.Primary))
            .Select(u => u.Party.Person.Organisation.CiiOrganisationId).FirstOrDefaultAsync();


          await _remoteCacheService.SetValueAsync<string>($"{CacheKeyConstant.UserOrganisation}-{_requestContext.RequestIntendedUserName}", intendedOrganisationId,
            new TimeSpan(0, _applicationConfigurationInfo.RedisCacheSettings.CacheExpirationInMinutes, 0));
        }

        isAuthorizedForOrganisation = _requestContext.CiiOrganisationId == intendedOrganisationId;
      }

      if (!isAuthorizedForOrganisation && !isCcsAdminRequest)
      {
        throw new ForbiddenException();
      }

      return true;
    }

    public async Task SendResetMfaNotificationAsync(MfaResetInfo mfaResetInfo, bool forceUserSignout = false)
    {
      var mfaEnabled = await _dataContext.User.Where(u => u.UserName == mfaResetInfo.UserName && !u.IsDeleted).
                        AnyAsync(u => u.MfaEnabled);

      if (mfaEnabled)
      {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);
        client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
        var url = "/security/mfa-reset-notifications";

        Dictionary<string, dynamic> requestData = new Dictionary<string, dynamic>
      {
        { "userName", mfaResetInfo.UserName},
        { "forceUserSignout", forceUserSignout}
      };

        HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
        { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

        var result = await client.PostAsync(url, data);
        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
          var errorMessage = await result.Content.ReadAsStringAsync();
          throw new CcsSsoException(errorMessage);
        }

        if (forceUserSignout)
        {
          await _remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + mfaResetInfo.UserName, true);
        }
      }
      else
      {
        throw new CcsSsoException("USER_MFA_NOT_ENABLED");
      }
    }
  }
}