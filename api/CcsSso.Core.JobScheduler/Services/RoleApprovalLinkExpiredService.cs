using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.DbModel.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly IServiceRoleGroupMapperService _serviceRoleGroupMapperService;
    private readonly AppSettings _appSettings;
    private readonly IWrapperConfigurationService _wrapperConfigurationService;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    public RoleApprovalLinkExpiredService(IServiceScopeFactory factory,
      ILogger<RoleApprovalLinkExpiredService> logger,
      IEmailSupportService emailSupportService,
      AppSettings appSettings,
      IWrapperConfigurationService wrapperConfigurationService,
      IWrapperUserService wrapperUserService,
      IWrapperOrganisationService wrapperOrganisationService)
    {
      _emailSupportService = emailSupportService;
      _logger = logger;
      _serviceRoleGroupMapperService = factory.CreateScope().ServiceProvider.GetRequiredService<IServiceRoleGroupMapperService>();
      _appSettings = appSettings;
      _wrapperConfigurationService = wrapperConfigurationService;
      _wrapperUserService = wrapperUserService;
      _wrapperOrganisationService = wrapperOrganisationService;
    }

    public async Task PerformJobAsync(List<UserAccessRolePendingDetailsInfo> pendingRoles)
    {
      var approvalRoleConfig = await _wrapperConfigurationService.GetRoleApprovalConfigurationsAsync();

      List<UserAccessRolePendingDetailsInfo> expiredUserAccessRolePendingList = new();
      List<UserAccessRolePendingDetailsInfo> relatedExpiredUserAccessRolePendingList = new();

      foreach (var role in pendingRoles)
      {

        var roleConfig = await GetRoleConfigAsync(approvalRoleConfig, role.OrganisationId);

        if (roleConfig == null)
          continue;

        var roleExpireTime = roleConfig.LastUpdatedOnUtc.AddMinutes(roleConfig.LinkExpiryDurationInMinute);

        if (roleExpireTime < DateTime.UtcNow)
        {
          var isExistInRelatedList = relatedExpiredUserAccessRolePendingList.Any(x => x.Id == role.Id);

          if (!isExistInRelatedList)
          {
            expiredUserAccessRolePendingList.Add(role);

            var relatedPendingRoles = pendingRoles.Where(x => x.Id != role.Id && x.UserId == role.UserId).ToList();
            relatedExpiredUserAccessRolePendingList.AddRange(relatedPendingRoles);
          }
        }
      }

      await ProcessRelatedExpiredUserAccessRolePending(relatedExpiredUserAccessRolePendingList);
      await ProcessExpiredUserAccessRolePending(expiredUserAccessRolePendingList, approvalRoleConfig);
    }

    private async Task ProcessRelatedExpiredUserAccessRolePending(List<UserAccessRolePendingDetailsInfo> relatedExpiredUserAccessRolePendingList)
    {
      _logger.LogInformation($"Total number of related expired Roles: {relatedExpiredUserAccessRolePendingList.Count()}");

      if (relatedExpiredUserAccessRolePendingList.Any())
      {
        await RemoveExpiredApprovalPendingRolesAsync(relatedExpiredUserAccessRolePendingList);
        _logger.LogInformation($"Successfully updated the related expired roles");
      }
    }

    private async Task ProcessExpiredUserAccessRolePending(List<UserAccessRolePendingDetailsInfo> expiredUserAccessRolePendingList, List<RoleApprovalConfigurationInfo> approvalRoleConfig)
    {
      _logger.LogInformation($"Total number of expired Roles: {expiredUserAccessRolePendingList.Count()}");

      if (expiredUserAccessRolePendingList.Any())
      {
        await RemoveExpiredApprovalPendingRolesAsync(expiredUserAccessRolePendingList);
        _logger.LogInformation($"Successfully updated the expired roles");

        _logger.LogInformation($"Sending email if it is eligible");
        await SendEmail(expiredUserAccessRolePendingList, approvalRoleConfig);
        _logger.LogInformation($"Finished sending email");
      }
    }

    private async Task RemoveExpiredApprovalPendingRolesAsync(List<UserAccessRolePendingDetailsInfo> userAccessRolePending)
    {
      UserAccessRolePendingFilterCriteria criteria = new UserAccessRolePendingFilterCriteria() { Status = UserPendingRoleStaus.Pending };
      var roleApprovalLink = await _wrapperUserService.GetUserAccessRolePendingDetails(criteria);
      var userAccessRolePendingExpiredList = roleApprovalLink.UserAccessRolePendingDetails.Where(u => !u.IsDeleted && userAccessRolePending.Select(x => x.Id).Contains(u.Id)).ToList();

      if (userAccessRolePendingExpiredList.Any())
      {
        var orgEligibleRoleIds = userAccessRolePendingExpiredList.Select(r => r.OrganisationEligibleRoleId).ToList();
        await _wrapperUserService.DeleteUserAccessRolePending(orgEligibleRoleIds);
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

        var emailList = await _wrapperUserService.GetUserByUserName(pendingNotification.UserName, pendingNotification.CreatedUserId);

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
          await _emailSupportService.SendRoleRejectedEmailAsync(email.UserName, pendingNotification.UserName, serviceName);
        }
      }

    }
    private async Task<RoleApprovalConfigurationInfo> GetRoleConfigAsync(List<RoleApprovalConfigurationInfo> approvalRoleConfig, int organisationId)
    {
      var orgDetails = await _wrapperOrganisationService.GetOrganisationDetailsById(organisationId);
      var orgEligibleRole = await _wrapperOrganisationService.GetOrganisationRoles(orgDetails.Detail.OrganisationId);
      var roleConfig = approvalRoleConfig.FirstOrDefault(config => orgEligibleRole.Any(orgRole => orgRole.CcsAccessRoleId == config.CcsAccessRoleId));

      return roleConfig;
    }

  }
}