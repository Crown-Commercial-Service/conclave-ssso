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
      var url = "/security/changepassword";

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
      var url = "/security/resetmfa_ticket";

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

      if (!isAuthorized)
      {
        throw new ForbiddenException();
      }

      return isAuthorized;
    }

    public async Task<bool> AuthorizeForOrganisationAsync(RequestType requestType)
    {
      var isCcsAdminRequest = _requestContext.Roles.Contains("ORG_USER_SUPPORT") || _requestContext.Roles.Contains("MANAGE_SUBSCRIPTIONS");
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
          intendedOrganisationId = await _dataContext.User.Where(u => u.UserName == _requestContext.RequestIntendedUserName)
            .Select(u => u.Party.Person.Organisation.CiiOrganisationId).FirstOrDefaultAsync();

          await _remoteCacheService.SetValueAsync<string>($"{CacheKeyConstant.UserOrganisation}-{_requestContext.RequestIntendedUserName}", intendedOrganisationId);
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
        var url = "/security/send_reset_mfa_notification";

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
