using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class UserProfileRoleApprovalService : IUserProfileRoleApprovalService
  {
    private readonly IDataContext _dataContext;
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;

    public UserProfileRoleApprovalService(IDataContext dataContext, ApplicationConfigurationInfo appConfigInfo, ICcsSsoEmailService ccsSsoEmailService)
    {
      _dataContext = dataContext;
      _appConfigInfo = appConfigInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
    }

    public Task<bool> UpdateUserRoleStatusAsync(UserRoleApprovalEditRequest userApprovalRequest)
    {
      var pendingRoleIds = userApprovalRequest.PendingRoleIds;
      var status = userApprovalRequest.Status;
      var linkForEmailTemplate = _appConfigInfo.ConclaveLoginUrl;

      foreach (var pendingRoleId in pendingRoleIds)
      {
        var pendingUserRole = _dataContext.UserAccessRolePending
          .FirstOrDefault(x => x.Id == pendingRoleId && !x.IsDeleted && x.Status == (int)UserPendingRoleStaus.Pending);

        if (pendingUserRole == null)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
        }

        var user = _dataContext.User
                  .Include(u => u.UserAccessRoles)
                  .FirstOrDefault(x => x.Id == pendingUserRole.UserId && !x.IsDeleted && x.UserType == UserType.Primary);

        if (user == null)
        {
          throw new RecordNotFoundException();
        }

        if (status == UserPendingRoleStaus.Rejected)
        {
          pendingUserRole.Status = (int)UserPendingRoleStaus.Rejected;
          pendingUserRole.IsDeleted = true;

        }
        else
        {
          pendingUserRole.Status = (int)UserPendingRoleStaus.Approved;
          pendingUserRole.IsDeleted = true;

          user.UserAccessRoles.Add(new UserAccessRole
          {
            UserId = user.Id,
            OrganisationEligibleRoleId = pendingUserRole.OrganisationEligibleRoleId
          });


          var orgEligibleRole = _dataContext.OrganisationEligibleRole.Include(or => or.CcsAccessRole)
                                          .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                                          .FirstOrDefault(u => u.Id == pendingUserRole.OrganisationEligibleRoleId! && u.IsDeleted);

          if (orgEligibleRole != null)
            linkForEmailTemplate = orgEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceUrl;
        }

        _dataContext.SaveChangesAsync();

        var emailList = new List<string>() { user.UserName };

        if (pendingUserRole.UserId != pendingUserRole.CreatedUserId)
        {
          var roleRequester = _dataContext.User
                  .Include(u => u.UserAccessRoles)
                  .FirstOrDefault(x => x.Id == pendingUserRole.CreatedUserId && !x.IsDeleted && x.UserType == UserType.Primary);

          if (roleRequester != null)
          {
            emailList.Add(roleRequester.UserName);
          }
        }

        foreach (var email in emailList)
        {
          if (status == UserPendingRoleStaus.Approved)
            _ccsSsoEmailService.SendRoleApprovedEmailAsync(user.UserName, linkForEmailTemplate);
          else
            _ccsSsoEmailService.SendRoleRejectedEmailAsync(user.UserName);
        }

      }
      return Task.FromResult(true);

    }
  }
}
