using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IServiceRoleGroupMapperService _serviceRoleGroupMapperService;

    public UserProfileRoleApprovalService(IDataContext dataContext, ApplicationConfigurationInfo appConfigInfo, ICcsSsoEmailService ccsSsoEmailService,
      IUserProfileHelperService userHelper, ICryptographyService cryptographyService, IServiceRoleGroupMapperService serviceRoleGroupMapperService)
    {
      _dataContext = dataContext;
      _appConfigInfo = appConfigInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
      _userHelper = userHelper;
      _cryptographyService = cryptographyService;
      _serviceRoleGroupMapperService = serviceRoleGroupMapperService;
    }

    public async Task<bool> UpdateUserRoleStatusAsync(UserRoleApprovalEditRequest userApprovalRequest)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      var pendingRoleIds = userApprovalRequest.PendingRoleIds;
      var status = userApprovalRequest.Status;
      
      if (status != UserPendingRoleStaus.Approved && status != UserPendingRoleStaus.Rejected)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidStatusInfo);
      }

      var pendingRole = await _dataContext.UserAccessRolePending
         .Where(x => pendingRoleIds.Contains(x.Id)).ToListAsync();

      if (pendingRole != null && pendingRole.Count() < pendingRoleIds.Length)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }

      // Get all services with pending approval role
      var serviceRoleGroupsWithApprovalRequiredRole = await _serviceRoleGroupMapperService.ServiceRoleGroupsWithApprovalRequiredRoleAsync();

      foreach (var pendingRoleId in pendingRoleIds)
      {
        var pendingUserRole = await _dataContext.UserAccessRolePending.Include(x => x.OrganisationEligibleRole).ThenInclude(r => r.CcsAccessRole)
                   .FirstOrDefaultAsync(x => x.Id == pendingRoleId);

        if (pendingUserRole == null)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
        }

        var isPendingRequest = !pendingUserRole.IsDeleted && pendingUserRole.Status == (int)UserPendingRoleStaus.Pending;

        var isOtherPendingRequest = await _dataContext.UserAccessRolePending
              .AnyAsync(x => !x.IsDeleted && x.UserId == pendingUserRole.UserId && x.Status == (int)UserPendingRoleStaus.Pending
                    && x.OrganisationEligibleRoleId == pendingUserRole.OrganisationEligibleRoleId && x.Id != pendingUserRole.Id);

        if (!isPendingRequest && !isOtherPendingRequest)
        {
            throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
        }

        var user = await _dataContext.User
                  .Include(u => u.UserAccessRoles).ThenInclude(u => u.OrganisationEligibleRole)
                  .FirstOrDefaultAsync(x => x.Id == pendingUserRole.UserId && !x.IsDeleted && x.UserType == UserType.Primary);

        if (user == null)
        {
          throw new ResourceNotFoundException();
        }

        if (isPendingRequest)
        {
          await ApproveRejectRoleRequest(status, serviceRoleGroupsWithApprovalRequiredRole, pendingUserRole, user);
          await SendApproveRejectRoleRequestEmail(status, pendingUserRole, user);
        }

        await UpdateRemaningRequestsOfUser(status, serviceRoleGroupsWithApprovalRequiredRole, pendingUserRole, user);
      }

      return await Task.FromResult(true);
    }

    private async Task SendApproveRejectRoleRequestEmail(UserPendingRoleStaus status, UserAccessRolePending pendingUserRole, User user)
    {
      string serviceName = await GetServiceNameForEmail(pendingUserRole);

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

      if (pendingUserRole.SendEmailNotification)
      {
        foreach (var email in emailList)
        {
          if (status == UserPendingRoleStaus.Approved)
            await _ccsSsoEmailService.SendRoleApprovedEmailAsync(email, user.UserName, serviceName, _appConfigInfo.ConclaveLoginUrl);
          else
            await _ccsSsoEmailService.SendRoleRejectedEmailAsync(email, user.UserName, serviceName);
        }
      }
    }

    private async Task<string> GetServiceNameForEmail(UserAccessRolePending pendingUserRole)
    {
      var serviceName = String.Empty;

      var orgEligibleRole = await _dataContext.OrganisationEligibleRole.Include(or => or.CcsAccessRole)
                                                  .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                                                  .FirstOrDefaultAsync(u => u.Id == pendingUserRole.OrganisationEligibleRoleId! && !u.IsDeleted);

      if (orgEligibleRole != null)
      {
        serviceName = orgEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName;
        if (_appConfigInfo.ServiceRoleGroupSettings.Enable)
        {
          var roleServiceInfo = await _serviceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(new List<int>() { orgEligibleRole.CcsAccessRoleId });
          serviceName = roleServiceInfo?.FirstOrDefault()?.Name;
        }
      }

      return serviceName;
    }

    private async Task UpdateRemaningRequestsOfUser(UserPendingRoleStaus status, List<CcsServiceRoleGroup> serviceRoleGroupsWithApprovalRequiredRole, UserAccessRolePending pendingUserRole, User user)
    {
      var userAccessRolePendingRequests = await _dataContext.UserAccessRolePending
                .Include(x => x.OrganisationEligibleRole).ThenInclude(r => r.CcsAccessRole)
                .Where(x => !x.IsDeleted && x.UserId == pendingUserRole.UserId && x.Status == (int)UserPendingRoleStaus.Pending
                      && x.OrganisationEligibleRoleId == pendingUserRole.OrganisationEligibleRoleId && x.Id != pendingUserRole.Id)
                .ToListAsync();

      foreach (var userAccessRolePendingRequest in userAccessRolePendingRequests)
      {
        await ApproveRejectRoleRequest(status, serviceRoleGroupsWithApprovalRequiredRole, userAccessRolePendingRequest, user);
      }
    }

    private async Task ApproveRejectRoleRequest(UserPendingRoleStaus status, List<CcsServiceRoleGroup> serviceRoleGroupsWithApprovalRequiredRole, UserAccessRolePending pendingUserRole, User user)
    {
      if (status == UserPendingRoleStaus.Rejected)
      {
        UpdateRoleRequestStatus(pendingUserRole, UserPendingRoleStaus.Rejected);
      }
      else
      {
        UpdateRoleRequestStatus(pendingUserRole, UserPendingRoleStaus.Approved);
        await AssignRequestedRoleToUser(serviceRoleGroupsWithApprovalRequiredRole, pendingUserRole, user);
      }
      await _dataContext.SaveChangesAsync();
    }

    private async Task AssignRequestedRoleToUser(List<CcsServiceRoleGroup> serviceRoleGroupsWithApprovalRequiredRole, UserAccessRolePending pendingUserRole, User user)
    {
      if (pendingUserRole.OrganisationUserGroupId == null)
      {
        var roleAlreadyExists = await _dataContext.UserAccessRole.FirstOrDefaultAsync(x => x.UserId == pendingUserRole.UserId && !x.IsDeleted && x.OrganisationEligibleRoleId == pendingUserRole.OrganisationEligibleRoleId);

        if (roleAlreadyExists == null)
        {
          user.UserAccessRoles.Add(new UserAccessRole
          {
            UserId = user.Id,
            OrganisationEligibleRoleId = pendingUserRole.OrganisationEligibleRoleId
          });

          // On role approval assign normal roles of service as well
          await AssignOtherServiceGroupRoleToUser(serviceRoleGroupsWithApprovalRequiredRole, pendingUserRole, user);
        }
      }
    }

    private async Task AssignOtherServiceGroupRoleToUser(List<CcsServiceRoleGroup> serviceRoleGroupsWithApprovalRequiredRole, UserAccessRolePending pendingUserRole, User user)
    {
      if (_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        var serviceGroup = serviceRoleGroupsWithApprovalRequiredRole.FirstOrDefault(x => x.CcsServiceRoleMappings.Any(r => r.CcsAccessRoleId == pendingUserRole.OrganisationEligibleRole.CcsAccessRoleId));
        var serviceMappingCcsRoleIds = serviceGroup.CcsServiceRoleMappings.Where(y => y.CcsAccessRole.ApprovalRequired == 0).Select(x => x.CcsAccessRoleId).ToList();

        var allOrgEligibleRoles = await _dataContext.OrganisationEligibleRole.Include(or => or.CcsAccessRole)
                                  .Where(x => !x.IsDeleted && x.OrganisationId == pendingUserRole.OrganisationEligibleRole.OrganisationId &&
                                          serviceMappingCcsRoleIds.Contains(x.CcsAccessRoleId)).ToListAsync();
        foreach (var orgRole in allOrgEligibleRoles)
        {
          if (!user.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == orgRole.Id && !x.IsDeleted))
          {
            user.UserAccessRoles.Add(new UserAccessRole
            {
              UserId = user.Id,
              OrganisationEligibleRoleId = orgRole.Id
            });
          }
        }
      }
    }

    private static void UpdateRoleRequestStatus(UserAccessRolePending pendingUserRole, UserPendingRoleStaus status)
    {
      pendingUserRole.Status = (int)status;
      pendingUserRole.IsDeleted = true;
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

      var userAccessRolePendingAllList = await _dataContext.UserAccessRolePending
        .Include(u => u.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Where(u => !u.IsDeleted && u.Status == (int)UserPendingRoleStaus.Pending
                    && u.OrganisationUserGroupId == null && u.UserId == userId)
        .ToListAsync();

      var approvalRoleConfig = await _dataContext.RoleApprovalConfiguration.Where(x => !x.IsDeleted).ToListAsync();
      List<UserAccessRolePending> validUserAccessRolePendingList = new();


      var userAccessRolePendingListResponse = userAccessRolePendingAllList.Select(u => new UserAccessRolePendingDetails()
      {
        RoleId = u.OrganisationEligibleRole.CcsAccessRole.Id,
        RoleKey = u.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
        RoleName = u.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
        Status = u.Status,
      }).ToList();

      return userAccessRolePendingListResponse;
    }

    public async Task RemoveApprovalPendingRolesAsync(string userName, string roleIds, int? groupId = null)
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
        throw new ResourceNotFoundException();
      }

      var userAccessRolePendingList = await _dataContext.UserAccessRolePending.Where(u => !u.IsDeleted && u.UserId == userId
                                      && u.OrganisationUserGroupId == groupId && roles.Contains(u.OrganisationEligibleRoleId.ToString())).ToListAsync();

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
      bool isLinkExpired = false;

      if (expirationTime < DateTime.UtcNow)
      {
        isLinkExpired = true;
      }

      var userAccessRoleUserDetails = await _dataContext.UserAccessRolePending
        .Include(u => u.User)
        .FirstOrDefaultAsync(u => u.Id == userAccessRolePendingId && u.User.UserType == UserType.Primary);

      var userAccessRolePendingRoleDetails = await _dataContext.UserAccessRolePending
        .Include(o => o.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .FirstOrDefaultAsync(u => u.Id == userAccessRolePendingId &&
        !u.OrganisationEligibleRole.IsDeleted && !u.OrganisationEligibleRole.CcsAccessRole.IsDeleted);

      if (userAccessRolePendingRoleDetails == default)
      {
        return new UserAccessRolePendingTokenDetails
        {
          UserName = userAccessRoleUserDetails.User.UserName,
          Status = (int)UserPendingRoleStaus.Expired
        };
      }
      else
      {
        var status = isLinkExpired ? (int)UserPendingRoleStaus.Expired : userAccessRolePendingRoleDetails.Status;

        if (status != (int)UserPendingRoleStaus.Pending)
        {
          var isPendingRequest = await _dataContext.UserAccessRolePending
            .AnyAsync(u => !u.IsDeleted && u.Status == (int)UserPendingRoleStaus.Pending
              && u.UserId == userAccessRolePendingRoleDetails.UserId 
              && u.OrganisationEligibleRoleId == userAccessRolePendingRoleDetails.OrganisationEligibleRoleId);

          status = isPendingRequest ? (int)UserPendingRoleStaus.Pending : status;
        }

        return new UserAccessRolePendingTokenDetails
        {
          Id = userAccessRolePendingRoleDetails.Id,
          UserName = userAccessRoleUserDetails.User.UserName,
          RoleId = userAccessRolePendingRoleDetails.OrganisationEligibleRole.CcsAccessRoleId,
          RoleName = userAccessRolePendingRoleDetails.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
          RoleKey = userAccessRolePendingRoleDetails.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
          Status = status
        };
      }
    }

    public async Task CreateUserRolesPendingForApprovalAsync(UserProfileEditRequestInfo userProfileRequestInfo, bool sendEmailNotification = true)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      var userName = userProfileRequestInfo.UserName;

      _userHelper.ValidateUserName(userName);

      var organisation = await _dataContext.Organisation
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);

      if (organisation == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      var roles = userProfileRequestInfo?.Detail?.RoleIds;

      if (roles == null || roles.Count == 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
        .Include(u => u.UserAccessRoles)
        .Include(u => u.UserAccessRolePending)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName.ToLower() == userName.ToLower() && u.UserType == UserType.Primary);

      if (user == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }

      if (user.Party.Person.Organisation.CiiOrganisationId != organisation.CiiOrganisationId)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationIdOrUserId);
      }

      var organisationId = user.Party.Person.OrganisationId;

      var org = await _dataContext.Organisation.FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == organisationId);

      if (org == null || user.UserName?.ToLower().Split('@')?[1] == org.DomainName?.ToLower())
      {
        throw new CcsSsoException(ErrorConstant.ErrorUserHasValidDomain);
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

      var groupId = userProfileRequestInfo.Detail.GroupId;

      if (groupId != null)
      {
        var group = await _dataContext.OrganisationUserGroup
        .Include(g => g.GroupEligibleRoles)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User)
        .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && g.Organisation.CiiOrganisationId == organisation.CiiOrganisationId);

        if (group == null)
        {
          throw new ResourceAlreadyExistsException(ErrorConstant.ErrorInvalidUserGroup);
        }

        var groupEligibleRoleIds = group.GroupEligibleRoles.Where(x => !x.IsDeleted).Select(x => x.OrganisationEligibleRoleId);
        if (!roles.All(roleId => groupEligibleRoleIds.Any(x => x == roleId)))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserGroupRole);
        }

        var isUserMemberOfGroup = group.UserGroupMemberships.Where(x => !x.IsDeleted).Any(x => x.UserId == user.Id);
        if (!isUserMemberOfGroup)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserInfo);
        }
      }

      // If GroupId is null then request is for user profile so we need check for user role exists
      if (groupId == null)
      {
        var userAccessRoles = await _dataContext.UserAccessRole
       .FirstOrDefaultAsync(uar => !uar.IsDeleted && uar.UserId == user.Id && roles.Contains(uar.OrganisationEligibleRoleId));

        if (userAccessRoles != null)
        {
          throw new ResourceAlreadyExistsException(ErrorConstant.ErrorUserRoleAlreadyExists);
        }
      }

      List<UserAccessRolePending> userAccessRolePendingToSendEmail = new List<UserAccessRolePending>();
      List<UserAccessRolePending> userAccessRolePendingAutoApproved = new List<UserAccessRolePending>();

      var userAccessRoleIds = await _dataContext.UserAccessRole.Where(x => !x.IsDeleted && x.UserId == user.Id).Select(x => x.OrganisationEligibleRoleId).ToListAsync();
      var userGroupApprovedRoleIds = await GetUserGroupApprovedRoleIds(user, groupId);

      roles.ForEach((roleId) =>
      {
        var isUserAccessRoleRequestPending = user.UserAccessRolePending.Any(x => !x.IsDeleted
          && x.OrganisationEligibleRoleId == Convert.ToInt32(roleId)
          && x.Status == (int)UserPendingRoleStaus.Pending);

        var isUserAccessRolePendingExist = user.UserAccessRolePending.Any(x => !x.IsDeleted
          && x.OrganisationEligibleRoleId == Convert.ToInt32(roleId)
          && x.OrganisationUserGroupId == groupId
          && x.Status == (int)UserPendingRoleStaus.Pending);

        if (!isUserAccessRolePendingExist)
        {
          var isRoleAssignedToUser = userAccessRoleIds.Any(x => x == roleId);
          var isRoleAssignedToUserFromGroup = userGroupApprovedRoleIds.Any(x => x == roleId);

          var userAccessRolePending = new UserAccessRolePending
          {
            OrganisationEligibleRoleId = roleId,
            Status = (int)UserPendingRoleStaus.Pending,
            OrganisationUserGroupId = groupId,
            SendEmailNotification = sendEmailNotification
          };

          if (isRoleAssignedToUser || isRoleAssignedToUserFromGroup)
          {
            userAccessRolePending.Status = (int)UserPendingRoleStaus.Approved;
            userAccessRolePending.IsDeleted = true;
            userAccessRolePendingAutoApproved.Add(userAccessRolePending);
          }

          user.UserAccessRolePending.Add(userAccessRolePending);

          if (!isUserAccessRoleRequestPending && userAccessRolePending.Status == (int)UserPendingRoleStaus.Pending)
          {
            userAccessRolePendingToSendEmail.Add(userAccessRolePending);
          }
        }
      });

      await _dataContext.SaveChangesAsync();

      if (userAccessRolePendingToSendEmail.Count > 0)
      {
        await SendEmailForApprovalPendingRolesAsync(user, userAccessRolePendingToSendEmail);
      }

      if (userAccessRolePendingAutoApproved.Count > 0)
      {
        await AssignRoleToUserForAutoApproved(user, userAccessRolePendingAutoApproved);
      }
    }

    private async Task AssignRoleToUserForAutoApproved(User user, List<UserAccessRolePending> userAccessRolePendingAutoApproved)
    {
      var serviceRoleGroupsWithApprovalRequiredRole = await _serviceRoleGroupMapperService.ServiceRoleGroupsWithApprovalRequiredRoleAsync();

      foreach (UserAccessRolePending userAccessRolePending in userAccessRolePendingAutoApproved)
      {
        await AssignRequestedRoleToUser(serviceRoleGroupsWithApprovalRequiredRole, userAccessRolePending, user);
      }

      await _dataContext.SaveChangesAsync();
    }

    private async Task<List<int>> GetUserGroupApprovedRoleIds(User user, int? groupId)
    {
      List<int> userGroupApprovedRoleIds = new List<int>();

      var userGroupIds = await _dataContext.UserGroupMembership.Where(x => !x.IsDeleted && x.UserId == user.Id).Select(x => x.OrganisationUserGroupId).ToListAsync();
      if (groupId != null)
      {
        userGroupIds = userGroupIds.Where(x => x != groupId).ToList();
      }
      var userGroupRoleIds = await _dataContext.OrganisationGroupEligibleRole.Where(x => !x.IsDeleted && userGroupIds.Contains(x.OrganisationUserGroupId)).Select(x => x.OrganisationEligibleRoleId).ToListAsync();

      userGroupRoleIds.ForEach((userGroupRoleId) =>
      {
        var lastUserAccessRolePedningRequest = user.UserAccessRolePending
        .OrderByDescending(o => o.CreatedOnUtc)
        .FirstOrDefault(x => x.OrganisationUserGroupId != null && x.OrganisationEligibleRoleId == userGroupRoleId);

        if (lastUserAccessRolePedningRequest?.Status == (int)UserPendingRoleStaus.Approved)
        {
          userGroupApprovedRoleIds.Add(userGroupRoleId);
        }
      });

      return userGroupApprovedRoleIds;
    }

    private async Task SendEmailForApprovalPendingRolesAsync(User user, List<UserAccessRolePending> userAccessRolePendingRequests)
    {
      string orgName = user.Party.Person.Organisation.LegalName;

      var roleApprovalConfigurations = await _dataContext.RoleApprovalConfiguration.ToListAsync();

      foreach (var userAccessRolePendingRequest in userAccessRolePendingRequests)
      {
        var userAccessRolePending = await _dataContext.UserAccessRolePending
          .Include(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
          .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserId == userAccessRolePendingRequest.UserId
              && u.Status == userAccessRolePendingRequest.Status
              && u.OrganisationUserGroupId == userAccessRolePendingRequest.OrganisationUserGroupId
              && u.OrganisationEligibleRoleId == userAccessRolePendingRequest.OrganisationEligibleRoleId);

        if (userAccessRolePending != null)
        {
          var roleApprovalConfiguration = roleApprovalConfigurations.FirstOrDefault(x => x.CcsAccessRoleId == userAccessRolePending.OrganisationEligibleRole.CcsAccessRoleId);

          if (roleApprovalConfiguration != null)
          {
            string roleApprovalInfo = "pendingid=" + userAccessRolePending.Id + "&exp=" + DateTime.UtcNow.AddMinutes(roleApprovalConfiguration.LinkExpiryDurationInMinute);
            var encryptedRoleApprovalInfo = _cryptographyService.EncryptString(roleApprovalInfo, _appConfigInfo.UserRoleApproval.RoleApprovalTokenEncryptionKey);
            string serviceName = userAccessRolePending.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName;

            if (_appConfigInfo.ServiceRoleGroupSettings.Enable)
            {
              var roleServiceInfo = await _serviceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(new List<int>() { userAccessRolePending.OrganisationEligibleRole.CcsAccessRole.Id });
              serviceName = roleServiceInfo?.FirstOrDefault()?.Name;
            }

            string[] notificationEmails = roleApprovalConfiguration.NotificationEmails.Split(',');

            foreach (var notificationEmail in notificationEmails)
            {
              await _ccsSsoEmailService.SendUserRoleApprovalEmailAsync(notificationEmail, user.UserName, orgName, serviceName, encryptedRoleApprovalInfo);
            }
          }
        }
      }
    }
    public async Task<List<UserServiceRoleGroupPendingDetails>> GetUserServiceRoleGroupsPendingForApprovalAsync(string userName)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      List<UserServiceRoleGroupPendingDetails> userServiceRoleGroups = new List<UserServiceRoleGroupPendingDetails>();

      var userRolesPendingForApproval = await this.GetUserRolesPendingForApprovalAsync(userName);

      var roleIds = userRolesPendingForApproval.Select(x => x.RoleId).ToList();

      var serviceRoleGroups = await _serviceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(roleIds);

      foreach (var userRolePendingForApproval in userRolesPendingForApproval)
      {
        var serviceRoleGroup = serviceRoleGroups.FirstOrDefault(x => x.CcsServiceRoleMappings.Any(r => r.CcsAccessRoleId == userRolePendingForApproval.RoleId));

        if (serviceRoleGroup != null)
        {
          userServiceRoleGroups.Add(new UserServiceRoleGroupPendingDetails()
          {
            Id = serviceRoleGroup.Id,
            Key = serviceRoleGroup.Key,
            Name = serviceRoleGroup.Name,
            Status = userRolePendingForApproval.Status
          });
        }
      }

      return userServiceRoleGroups;
    }

    public async Task<UserAccessServiceRoleGroupPendingTokenDetails> VerifyAndReturnServiceRoleGroupApprovalTokenDetailsAsync(string token)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable || !_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var tokenDetails = await VerifyAndReturnRoleApprovalTokenDetailsAsync(token);
      CcsServiceRoleGroup serviceRoleGroupDetails = new();

      if (tokenDetails?.RoleId != null)
      {
        var serviceRoleGroups = await _serviceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(new List<int> { tokenDetails.RoleId });
        serviceRoleGroupDetails = serviceRoleGroups.FirstOrDefault();
      }

      return new UserAccessServiceRoleGroupPendingTokenDetails()
      {
        Id = tokenDetails.Id,
        Key = serviceRoleGroupDetails?.Key,
        Name = serviceRoleGroupDetails?.Name,
        UserName = tokenDetails.UserName,
        Status = tokenDetails.Status
      };
    }

  }
}
