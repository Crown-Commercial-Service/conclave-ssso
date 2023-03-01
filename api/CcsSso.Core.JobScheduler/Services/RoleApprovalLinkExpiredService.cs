using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Jobs;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CcsSso.Core.JobScheduler.Services
{
  public class RoleApprovalLinkExpiredService : IRoleApprovalLinkExpiredService
  {
    private readonly IDataContext _dataContext;
    private readonly ILogger<RoleApprovalLinkExpiredService> _logger;
    private readonly IEmailSupportService _emailSupportService;
    private readonly IServiceRoleGroupMapperService _serviceRoleGroupMapperService;
    private readonly AppSettings _appSettings;

    public RoleApprovalLinkExpiredService(IServiceScopeFactory factory,
      ILogger<RoleApprovalLinkExpiredService> logger,
       IEmailSupportService emailSupportService,
       AppSettings appSettings)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _emailSupportService = emailSupportService;
      _logger = logger;
      _serviceRoleGroupMapperService = factory.CreateScope().ServiceProvider.GetRequiredService<IServiceRoleGroupMapperService>();
      _appSettings = appSettings;
    }

    public async Task PerformJobAsync(List<UserAccessRolePending> pendingRoles)
    {

      var approvalRoleConfig = await _dataContext.RoleApprovalConfiguration.Where(x => !x.IsDeleted).ToListAsync();

      List<UserAccessRolePending> expiredUserAccessRolePendingList = new();

      foreach (var role in pendingRoles)
      {
        var roleExpireTime = role.LastUpdatedOnUtc.AddMinutes(approvalRoleConfig.FirstOrDefault(x => x.CcsAccessRoleId ==
           role.OrganisationEligibleRole.CcsAccessRole.Id).LinkExpiryDurationInMinute);

        if (roleExpireTime < DateTime.UtcNow)
        {
          expiredUserAccessRolePendingList.Add(role);
        }
      }

      _logger.LogInformation($"Total number of expired Roles: {expiredUserAccessRolePendingList.Count()}");

      if (expiredUserAccessRolePendingList.Any())
      {
        await RemoveExpiredApprovalPendingRolesAsync(expiredUserAccessRolePendingList);
        _logger.LogInformation($"Successfully updated the expired roles");

        _logger.LogInformation($"Sending email if it is eligible");
        await SendEmail(expiredUserAccessRolePendingList);
        _logger.LogInformation($"Finished sending email");

      }
    }

    private async Task RemoveExpiredApprovalPendingRolesAsync(List<UserAccessRolePending> userAccessRolePending)
    {
      var userAccessRolePendingExpiredList = await _dataContext.UserAccessRolePending.Where(u => !u.IsDeleted && userAccessRolePending.Select(x => x.Id).Contains(u.Id)).ToListAsync();

      if (userAccessRolePendingExpiredList.Any())
      {
        userAccessRolePendingExpiredList.ForEach(l => { l.IsDeleted = true; l.Status = (int)UserPendingRoleStaus.Expired; });
        await _dataContext.SaveChangesAsync();

        
      }
    }

    private async Task SendEmail(List<UserAccessRolePending> userAccessRolePending)
    {

      foreach (var pendingNotification in userAccessRolePending)
      {
        if (!pendingNotification.SendEmailNotification)
        {
          continue;
        }

        var user = await _dataContext.User
                 .Include(u => u.UserAccessRoles)
                 .FirstOrDefaultAsync(x => x.Id == pendingNotification.UserId && !x.IsDeleted && x.UserType == UserType.Primary);

        var emailList = new List<string>() { user.UserName };

        if (pendingNotification.UserId != pendingNotification.CreatedUserId)
        {
          var roleRequester = await _dataContext.User
                  .FirstOrDefaultAsync(x => x.Id == pendingNotification.CreatedUserId && !x.IsDeleted && x.UserType == UserType.Primary);

          if (roleRequester != null)
          {
            emailList.Add(roleRequester.UserName);
          }
        }

        var serviceName = string.Empty;

        var orgEligibleRole = await _dataContext.OrganisationEligibleRole.Include(or => or.CcsAccessRole)
                                       .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                                       .FirstOrDefaultAsync(u => u.Id == pendingNotification.OrganisationEligibleRoleId! && !u.IsDeleted);

        if (orgEligibleRole != null)
        {
          serviceName = orgEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName;

          if (_appSettings.ServiceRoleGroupSettings.Enable)
          {
            var roleServiceInfo = await _serviceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(new List<int>() { orgEligibleRole.CcsAccessRole.Id });
            serviceName = roleServiceInfo?.FirstOrDefault()?.Name;
          }
        }

        foreach (var email in emailList)
        {
          await _emailSupportService.SendRoleRejectedEmailAsync(email, user.UserName, serviceName);
        }
      }

    }

  }
}
