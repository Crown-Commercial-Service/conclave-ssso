using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Services
{
  public class RoleApprovalLinkExpiredService : IRoleApprovalLinkExpiredService
  {
    private readonly ILogger<RoleApprovalLinkExpiredService> _logger;
    private readonly IEmailSupportService _emailSupportService;
    private readonly AppSettings _appSettings;
    private readonly IWrapperConfigurationService _wrapperConfigurationService;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private UserAccessRolePendingRequestDetails PendingRolesList { get; set; }
    public RoleApprovalLinkExpiredService(ILogger<RoleApprovalLinkExpiredService> logger,
      IEmailSupportService emailSupportService,
      AppSettings appSettings,
      IWrapperConfigurationService wrapperConfigurationService,
      IWrapperUserService wrapperUserService,
      IWrapperOrganisationService wrapperOrganisationService)
    {
      _emailSupportService = emailSupportService;
      _logger = logger;
      _appSettings = appSettings;
      _wrapperConfigurationService = wrapperConfigurationService;
      _wrapperUserService = wrapperUserService;
      _wrapperOrganisationService = wrapperOrganisationService;
      PendingRolesList = new UserAccessRolePendingRequestDetails();
    }


    public async Task PerformJobAsync(List<UserAccessRolePendingDetailsInfo> pendingRoles)
    {
      PendingRolesList.UserAccessRolePendingDetailsInfo = pendingRoles;
      var approvalRoleConfig = await _wrapperConfigurationService.GetRoleApprovalConfigurationsAsync();

      foreach (var approvalRole in approvalRoleConfig)
      {
        _logger.LogInformation($"****** Approval role config role id: {approvalRole.CcsAccessRoleId}, expired duration: {approvalRole.LinkExpiryDurationInMinute}, " +
          $"email to: {approvalRole?.NotificationEmails} ");
      }

      List<UserAccessRolePendingDetailsInfo> expiredUserAccessRolePendingList = new();
      List<UserAccessRolePendingDetailsInfo> relatedExpiredUserAccessRolePendingList = new();

      foreach (var role in pendingRoles)
      {

        var roleConfig = await GetRoleConfigAsync(approvalRoleConfig, role.OrganisationId);

        if (roleConfig == null)
          continue;

        _logger.LogInformation($"****** Found role config matching org role: {roleConfig.CcsAccessRoleId}, expired minutes: {roleConfig.LinkExpiryDurationInMinute}");

        var roleExpireTime = role.LastUpdatedOnUtc.AddMinutes(roleConfig.LinkExpiryDurationInMinute);

        if (roleExpireTime < DateTime.UtcNow)
        {
          var isExistInRelatedList = relatedExpiredUserAccessRolePendingList.Any(x => x.Id == role.Id);

          if (!isExistInRelatedList)
          {
            expiredUserAccessRolePendingList.Add(role);

            var relatedPendingRoles = pendingRoles.Where(x => x.Id != role.Id && x.UserName == role.UserName).ToList();
            relatedExpiredUserAccessRolePendingList.AddRange(relatedPendingRoles);
          }
        }
      }

      await ProcessRelatedExpiredUserAccessRolePending(relatedExpiredUserAccessRolePendingList);
      await ProcessExpiredUserAccessRolePending(expiredUserAccessRolePendingList, approvalRoleConfig);
    }

    private async Task ProcessRelatedExpiredUserAccessRolePending(List<UserAccessRolePendingDetailsInfo> relatedExpiredUserAccessRolePendingList)
    {
      _logger.LogInformation($"****** Total number of related expired Roles: {relatedExpiredUserAccessRolePendingList.Count()}");

      if (relatedExpiredUserAccessRolePendingList.Any())
      {
        await RemoveExpiredApprovalPendingRolesAsync(relatedExpiredUserAccessRolePendingList);
        _logger.LogInformation($"****** Successfully updated the related expired roles.");
      }
    }

    private async Task ProcessExpiredUserAccessRolePending(List<UserAccessRolePendingDetailsInfo> expiredUserAccessRolePendingList, List<RoleApprovalConfigurationInfo> approvalRoleConfig)
    {
      _logger.LogInformation($"****** Total number of expired roles: {expiredUserAccessRolePendingList.Count()}");

      if (expiredUserAccessRolePendingList.Any())
      {
        await RemoveExpiredApprovalPendingRolesAsync(expiredUserAccessRolePendingList);
        _logger.LogInformation($"****** Successfully updated the expired roles.");

        _logger.LogInformation($"****** Sending email if it is eligible.");
        await SendEmail(expiredUserAccessRolePendingList, approvalRoleConfig);
        _logger.LogInformation($"****** Finished sending email.");
      }
    }

    private async Task RemoveExpiredApprovalPendingRolesAsync(List<UserAccessRolePendingDetailsInfo> userAccessRolePending)
    {
      var userAccessRolePendingExpiredList = PendingRolesList.UserAccessRolePendingDetailsInfo
        .Where(pr => !pr.IsDeleted && userAccessRolePending.Select(x => x.Id).Contains(pr.Id)).ToList();

      if (userAccessRolePendingExpiredList.Any())
      {
        PendingRolesList.UserAccessRolePendingDetailsInfo.ForEach(async pr =>
        {
          var orgEligibleRoleIds = userAccessRolePendingExpiredList.Select(r => r.OrganisationEligibleRoleId).ToList();
          await _wrapperUserService.RemoveApprovalPendingRoles(pr.UserName, orgEligibleRoleIds, UserPendingRoleStaus.Expired);
        });

      }
    }

    private async Task SendEmail(List<UserAccessRolePendingDetailsInfo> userAccessRolePending, List<RoleApprovalConfigurationInfo> approvalRoleConfig)
    {

      foreach (var pendingNotification in userAccessRolePending)
      {
        if (!pendingNotification.SendEmailNotification)
        {
          continue;
        }

        var emailList = new List<string>() { pendingNotification.UserName };
        if (pendingNotification.UserName != pendingNotification.CreatedBy)
        {
          if (pendingNotification.CreatedBy != null)
          {
            emailList.Add(pendingNotification.CreatedBy);
          }
        }

        var serviceName = string.Empty;
        var roleConfig = await GetRoleConfigAsync(approvalRoleConfig, pendingNotification.OrganisationId);

        if (roleConfig != null)
        {

          if (_appSettings.ServiceRoleGroupSettings.Enable)
          {
            var roleServiceInfo = await _wrapperConfigurationService.GetServiceRoleGroupsRequireApproval();
            serviceName = roleServiceInfo?.FirstOrDefault()?.Name;
          }
        }

        foreach (var email in emailList)
        {
          await _emailSupportService.SendRoleRejectedEmailAsync(email, pendingNotification.UserName, serviceName);
        }
      }

    }

    private async Task<RoleApprovalConfigurationInfo> GetRoleConfigAsync(List<RoleApprovalConfigurationInfo> approvalRoleConfig, string organisationId)
    {
      try
      {
        // var orgDetails = await _wrapperOrganisationService.GetOrganisationDetailsById(CiiOrganisationId);
        var orgEligibleRole = await _wrapperOrganisationService.GetOrganisationRoles(organisationId);

        _logger.LogInformation($"****** Org roles found :{orgEligibleRole.Count()} for org: {organisationId}");

        var roleConfig = approvalRoleConfig.FirstOrDefault(config => orgEligibleRole.Any(orgRole => orgRole.CcsAccessRoleId == config.CcsAccessRoleId));

        return roleConfig;
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error getting org roles: {JsonConvert.SerializeObject(e)}");
        return null;
      }
    }

  }
}