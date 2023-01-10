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

    public async Task<bool> UpdateUserRoleStatusAsync(UserRoleApprovalEditRequest userApprovalRequest)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      var pendingRoleIds = userApprovalRequest.PendingRoleIds;
      var status = userApprovalRequest.Status;
      var linkForEmailTemplate = _appConfigInfo.ConclaveLoginUrl;
      var serviceName = String.Empty; 

      if (status != UserPendingRoleStaus.Approved && status != UserPendingRoleStaus.Rejected)
      {
        throw new InvalidOperationException();
      }

      // The following apporach works but need to fetch all the pending roles. It is for buddy check to find the best approach. 
      //var pendingRole = _dataContext.UserAccessRolePending
      //   .Where(x => !x.IsDeleted && x.Status == (int)UserPendingRoleStaus.Pending).ToList();

      //if (!pendingRoleIds.All(pending => pendingRole.Any(y => y.Id == pending)))
      //{
      //  throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      //}

      var pendingRole = await _dataContext.UserAccessRolePending
         .Where(x => pendingRoleIds.Contains(x.Id) && !x.IsDeleted && x.Status == (int)UserPendingRoleStaus.Pending).ToListAsync();

      if (pendingRole != null && pendingRole.Count() < pendingRoleIds.Length)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      foreach (var pendingRoleId in pendingRoleIds)
      {
        var pendingUserRole = await _dataContext.UserAccessRolePending
                   .FirstOrDefaultAsync(x => x.Id == pendingRoleId && !x.IsDeleted && x.Status == (int)UserPendingRoleStaus.Pending);

        var user = await _dataContext.User
                  .Include(u => u.UserAccessRoles)
                  .FirstOrDefaultAsync(x => x.Id == pendingUserRole.UserId && !x.IsDeleted && x.UserType == UserType.Primary);

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


          var orgEligibleRole = await _dataContext.OrganisationEligibleRole.Include(or => or.CcsAccessRole)
                                          .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                                          .FirstOrDefaultAsync(u => u.Id == pendingUserRole.OrganisationEligibleRoleId! && u.IsDeleted);

          if (orgEligibleRole != null)
          {
            linkForEmailTemplate = orgEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceUrl;
            serviceName = orgEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName;
          }
        }

        await _dataContext.SaveChangesAsync();

        var emailList = new List<string>() { user.UserName };

        if (pendingUserRole.UserId != pendingUserRole.CreatedUserId)
        {
          var roleRequester =await _dataContext.User
                  .FirstOrDefaultAsync(x => x.Id == pendingUserRole.CreatedUserId && !x.IsDeleted && x.UserType == UserType.Primary);

          if (roleRequester != null)
          {
            emailList.Add(roleRequester.UserName);
          }
        }

        foreach (var email in emailList)
        {
          if (status == UserPendingRoleStaus.Approved)
            await _ccsSsoEmailService.SendRoleApprovedEmailAsync(email, serviceName, linkForEmailTemplate);
          else
            await _ccsSsoEmailService.SendRoleRejectedEmailAsync(email);
        }

      }
      return await Task.FromResult(true);

    }
  }
}
