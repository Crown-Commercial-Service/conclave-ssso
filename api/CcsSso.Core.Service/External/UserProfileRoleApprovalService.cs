using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Shared.Contracts;
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
    private readonly IUserProfileHelperService _userHelper;
    private readonly ICryptographyService _cryptographyService;

    public UserProfileRoleApprovalService(IDataContext dataContext, ApplicationConfigurationInfo appConfigInfo, ICcsSsoEmailService ccsSsoEmailService,
      IUserProfileHelperService userHelper, ICryptographyService cryptographyService)
    {
      _dataContext = dataContext;
      _appConfigInfo = appConfigInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
      _userHelper = userHelper;
      _cryptographyService = cryptographyService;
    }

    public async Task<bool> UpdateUserRoleStatusAsync(UserRoleApprovalEditRequest userApprovalRequest)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      var pendingRoleIds = userApprovalRequest.PendingRoleIds;
      var status = userApprovalRequest.Status;       
      var serviceName = String.Empty;

      if (status != UserPendingRoleStaus.Approved && status != UserPendingRoleStaus.Rejected)
      {
        throw new InvalidOperationException();
      }

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
        }

        await _dataContext.SaveChangesAsync();

        var orgEligibleRole = await _dataContext.OrganisationEligibleRole.Include(or => or.CcsAccessRole)
                                          .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                                          .FirstOrDefaultAsync(u => u.Id == pendingUserRole.OrganisationEligibleRoleId! && !u.IsDeleted);

        if (orgEligibleRole != null)
        {
          serviceName = orgEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName;
        }

        var emailList = new List<string>() { user.UserName };

        if (pendingUserRole.UserId != pendingUserRole.CreatedUserId)
        {
          var roleRequester = await _dataContext.User
                  .FirstOrDefaultAsync(x => x.Id == pendingUserRole.CreatedUserId && !x.IsDeleted && x.UserType == UserType.Primary);

          if (roleRequester != null)
          {
            emailList.Add(roleRequester.UserName);
          }
        }

        foreach (var email in emailList)
        {
          if (status == UserPendingRoleStaus.Approved)
            await _ccsSsoEmailService.SendRoleApprovedEmailAsync(email, serviceName, _appConfigInfo.ConclaveLoginUrl);
          else
            await _ccsSsoEmailService.SendRoleRejectedEmailAsync(email, serviceName);
        }

      }
      return await Task.FromResult(true);

    }

    public async Task<List<UserAccessRolePendingDetails>> GetUserRolesPendingForApprovalAsync(string userName)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      _userHelper.ValidateUserName(userName);

      var userId = await _dataContext.User.Where(u => u.UserName.ToLower() == userName.ToLower() && u.UserType == UserType.Primary && !u.IsDeleted)
                   .Select(U => U.Id).FirstOrDefaultAsync();

      if (userId == 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }

      var userAccessRolePendingList = await _dataContext.UserAccessRolePending
        .Include(u => u.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Where(u => !u.IsDeleted && u.Status == (int)UserPendingRoleStaus.Pending && u.UserId == userId)
        .Select(u => new UserAccessRolePendingDetails()
        {
          Status = u.Status,
          RoleKey = u.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
          RoleName = u.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName
        })
        .ToListAsync();

      return userAccessRolePendingList;
    }

    public async Task RemoveApprovalPendingRolesAsync(string userName, string roleIds)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }
      if (string.IsNullOrWhiteSpace(roleIds))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }

      string[] roles = roleIds.Split(',');

      _userHelper.ValidateUserName(userName);

      var userId = await _dataContext.User.Where(u => u.UserName.ToLower() == userName.ToLower() && u.UserType == UserType.Primary && !u.IsDeleted)
                         .Select(u => u.Id).FirstOrDefaultAsync();

      if (userId == 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }

      var userAccessRolePendingList = await _dataContext.UserAccessRolePending.Where(u => !u.IsDeleted && u.UserId == userId &&
                                      roles.Contains(u.OrganisationEligibleRoleId.ToString())).ToListAsync();

      if (userAccessRolePendingList.Any())
      {
        userAccessRolePendingList.ForEach(l => { l.IsDeleted = true; l.Status = (int)UserPendingRoleStaus.Removed; });
        await _dataContext.SaveChangesAsync();
      }
      else
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }
    }

    public async Task<UserAccessRolePendingTokenDetails> VerifyAndReturnRoleApprovalTokenDetailsAsync(string token)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      token = token?.Replace(" ", "+");
      // Decrypt token
      string decryptedToken = _cryptographyService.DecryptString(token, _appConfigInfo.UserRoleApproval.RoleApprovalTokenEncryptionKey);

      if (string.IsNullOrWhiteSpace(decryptedToken))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      //validate token expiration
      Dictionary<string, string> tokenDetails = decryptedToken.Split('&').Select(value => value.Split('='))
                                                  .ToDictionary(pair => pair[0], pair => pair[1]);
      if (!tokenDetails.ContainsKey("pendingid") || !tokenDetails.ContainsKey("exp"))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      int userAccessRolePendingId = Convert.ToInt32(tokenDetails["pendingid"]);
      DateTime expirationTime = Convert.ToDateTime(tokenDetails["exp"]);

      if (expirationTime < DateTime.UtcNow)
      {
        throw new CcsSsoException(ErrorConstant.ErrorLinkExpired);
      }

      var userAccessRolePendingDetails = await _dataContext.UserAccessRolePending
        .Include(u => u.User)
        .Include(o => o.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .FirstOrDefaultAsync(u => u.Id == userAccessRolePendingId && u.User.UserType == UserType.Primary && !u.OrganisationEligibleRole.IsDeleted);

      if (userAccessRolePendingDetails == default)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      return new UserAccessRolePendingTokenDetails
      {
        Id = userAccessRolePendingDetails.Id,
        UserName = userAccessRolePendingDetails.User.UserName,
        RoleName = userAccessRolePendingDetails.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
        RoleKey = userAccessRolePendingDetails.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
        Status = userAccessRolePendingDetails.Status
      };
    }

    public async Task CreateUserRolesPendingForApprovalAsync(UserProfileEditRequestInfo userProfileRequestInfo)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      var userName = userProfileRequestInfo.UserName;

      _userHelper.ValidateUserName(userName);

      var roles = userProfileRequestInfo.Detail.RoleIds;

      if (roles == null || roles.Count == 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
        .Include(u => u.UserAccessRolePending)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName.ToLower() == userName.ToLower() && u.UserType == UserType.Primary);

      if (user == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }

      var organisationId = user.Party.Person.OrganisationId;

      var org = await _dataContext.Organisation.FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == organisationId);

      if (org == null || user.UserName?.Split('@')?[1] == org.DomainName)
      {
        throw new InvalidOperationException();
      }

      var organisationEligibleRoles = await _dataContext.OrganisationEligibleRole
        .Where(oer => !oer.IsDeleted && oer.OrganisationId == organisationId)
        .ToListAsync();

      if (!roles.All(roleId => organisationEligibleRoles.Any(oer => oer.Id == Convert.ToInt32(roleId))))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }

      var roleRequiredApprovalIds = await _dataContext.CcsAccessRole
        .Where(x => !x.IsDeleted && x.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired)
        .Select(x => x.Id)
        .ToListAsync();

      var organisationRoleRequiredApprovalIds = organisationEligibleRoles
        .Where(x => roleRequiredApprovalIds.Contains(x.CcsAccessRoleId))
        .Select(x => x.Id);

      if (!roles.All(roleId => organisationRoleRequiredApprovalIds.Any(x => x == roleId)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }

      List<UserAccessRolePending> userAccessRolePendingList = new List<UserAccessRolePending>();

      List<int> rolesToSendEmail = new List<int>();

      roles.ForEach((roleId) =>
      {
        var isUserAccessRolePendingExist = user.UserAccessRolePending.Any(x => !x.IsDeleted
          && x.OrganisationEligibleRoleId == Convert.ToInt32(roleId)
          && x.Status == (int)UserPendingRoleStaus.Pending);

        if (!isUserAccessRolePendingExist)
        {
          user.UserAccessRolePending.Add(new UserAccessRolePending
          {
            OrganisationEligibleRoleId = roleId,
            Status = (int)UserPendingRoleStaus.Pending
          });
          rolesToSendEmail.Add(roleId);
        }
      });

      await _dataContext.SaveChangesAsync();

      if (rolesToSendEmail.Count > 0)
      {
        await SenEmailForApprovalPendingRolesAsync(user, rolesToSendEmail);
      }
    }

    private async Task SenEmailForApprovalPendingRolesAsync(User user, List<int> roles)
    {
      var userAccessRolePendingList = await _dataContext.UserAccessRolePending
        .Include(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
        .Where(u => !u.IsDeleted && u.UserId == user.Id && u.Status == (int)UserPendingRoleStaus.Pending && roles.Contains(u.OrganisationEligibleRoleId))
        .ToListAsync();

      string orgName = user.Party.Person.Organisation.LegalName;

      if (userAccessRolePendingList.Any())
      {
        var roleApprovalConfigurations = await _dataContext.RoleApprovalConfiguration.ToListAsync();

        foreach (var userAccessRolePending in userAccessRolePendingList)
        {
          var roleApprovalConfiguration = roleApprovalConfigurations.FirstOrDefault(x => x.CcsAccessRoleId == userAccessRolePending.OrganisationEligibleRole.CcsAccessRoleId);

          if (roleApprovalConfiguration != null)
          {
            string roleApprovalInfo = "pendingid=" + userAccessRolePending.Id + "&exp=" + DateTime.UtcNow.AddMinutes(roleApprovalConfiguration.LinkExpiryDurationInMinute);
            var encryptedRoleApprovalInfo = _cryptographyService.EncryptString(roleApprovalInfo, _appConfigInfo.UserRoleApproval.RoleApprovalTokenEncryptionKey);
            string serviceName = userAccessRolePending.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName;

            string[] notificationEmails = roleApprovalConfiguration.NotificationEmails.Split(',');

            foreach (var notificationEmail in notificationEmails)
            {
              await _ccsSsoEmailService.SendUserRoleApprovalEmailAsync(notificationEmail, user.UserName, orgName, serviceName, encryptedRoleApprovalInfo);
            }
          }
        }
      }
    }

  }
}
