using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public partial class DelegationAuditEventService : IDelegationAuditEventService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dateTimeService;
    private readonly IServiceRoleGroupMapperService _rolesToServiceRoleGroupMapperService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IUserProfileHelperService _userHelper;
    private readonly IExternalHelperService _externalHelperService;

    public DelegationAuditEventService(IDataContext dataContext, IDateTimeService dateTimeService,
      IServiceRoleGroupMapperService rolesToServiceRoleGroupMapperService, ApplicationConfigurationInfo applicationConfigurationInfo,
      IUserProfileHelperService userHelper,IExternalHelperService externalHelperService)
    {
      _dataContext = dataContext;
      _dateTimeService = dateTimeService;
      _rolesToServiceRoleGroupMapperService = rolesToServiceRoleGroupMapperService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _userHelper = userHelper;
      _externalHelperService = externalHelperService;
    }

    public async Task CreateDelegationAuditEventsAsync(List<DelegationAuditEventInfo> delegationAuditEventInfoList)
    {
      List<DelegationAuditEvent> delegationAuditEvents = new List<DelegationAuditEvent>();

      foreach (var delegationAuditEventInfo in delegationAuditEventInfoList)
      {
        var delegationAuditEvent = new DelegationAuditEvent
        {
          GroupId = delegationAuditEventInfo.GroupId,
          UserId = delegationAuditEventInfo.UserId,
          PreviousDelegationStartDate = delegationAuditEventInfo.PreviousDelegationStartDate,
          PreviousDelegationEndDate = delegationAuditEventInfo.PreviousDelegationEndDate,
          NewDelegationStartDate = delegationAuditEventInfo.NewDelegationStartDate,
          NewDelegationEndDate = delegationAuditEventInfo.NewDelegationEndDate,
          Roles = delegationAuditEventInfo.Roles,
          EventType = delegationAuditEventInfo.EventType,
          ActionedOnUtc = _dateTimeService.GetUTCNow(),
          ActionedBy = delegationAuditEventInfo.ActionedBy,
          ActionedByUserName = delegationAuditEventInfo.ActionedByUserName,
          ActionedByFirstName = delegationAuditEventInfo.ActionedByFirstName,
          ActionedByLastName = delegationAuditEventInfo.ActionedByLastName,
        };

        delegationAuditEvents.Add(delegationAuditEvent);
      }

      _dataContext.DelegationAuditEvent.AddRange(delegationAuditEvents);

      await _dataContext.SaveChangesAsync();

    }

    public async Task<DelegationAuditEventoServiceRoleGroupInfListResponse> GetDelegationAuditEventsListAsync(string userName, string organisationId, ResultSetCriteria resultSetCriteria)
    {
      _userHelper.ValidateUserName(userName);
      if (string.IsNullOrWhiteSpace(organisationId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName &&
        u.UserType == UserType.Delegation &&
        u.Party.Person.Organisation.CiiOrganisationId == organisationId);

      if (user == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegation);
      }

      var auditLogs = await GetDelegationAuditLogs(user.Id);
      int pageCount;
      var result = UtilityHelper.GetPagedResult(auditLogs, resultSetCriteria.CurrentPage, resultSetCriteria.PageSize, out pageCount);
      return new DelegationAuditEventoServiceRoleGroupInfListResponse
      {
        CurrentPage = resultSetCriteria.CurrentPage,
        PageCount = pageCount,
        RowCount = auditLogs.Count,
        DelegationAuditEventServiceRoleGroupList = result.Cast<DelegationAuditEventServiceRoleGroupResponseInfo>().ToList() ?? new List<DelegationAuditEventServiceRoleGroupResponseInfo>()
      };
    }

    private async Task<List<DelegationAuditEventServiceRoleGroupResponseInfo>> GetDelegationAuditLogs(int userId)
    {
      List<DelegationAuditEventServiceRoleGroupResponseInfo> auditEventInfos = new();

      var auditEvents = await _dataContext.DelegationAuditEvent
        .Where(c => c.UserId == userId)
        .ToListAsync();

      
      var allRoles =await _externalHelperService.GetCcsAccessRoles();

      foreach (var auditEvent in auditEvents)
      {
        auditEventInfos.AddRange(await GetDelegationServiceRoleGroupLogs(auditEvent, allRoles));
      }

      auditEventInfos = auditEventInfos.OrderByDescending(x => x.ActionedOnUtc).ToList();
      return auditEventInfos;
    }

    private static DelegationAuditEventResponseInfo GetDelegationAuditEventInfo(DelegationAuditEvent auditEvent)
    {
      return new DelegationAuditEventResponseInfo
      {
        GroupId = auditEvent.GroupId,
        PreviousDelegationStartDate = auditEvent.PreviousDelegationStartDate,
        PreviousDelegationEndDate = auditEvent.PreviousDelegationEndDate,
        NewDelegationStartDate = auditEvent.NewDelegationStartDate,
        NewDelegationEndDate = auditEvent.NewDelegationEndDate,
        EventType = auditEvent.EventType,
        ActionedOnUtc = auditEvent.ActionedOnUtc,
        ActionedBy = auditEvent.ActionedBy,
        ActionedByUserName = auditEvent.ActionedByUserName,
        ActionedByFirstName = auditEvent.ActionedByFirstName,
        ActionedByLastName = auditEvent.ActionedByLastName,
        Role = string.Empty,
        RoleKey = string.Empty,
        ServiceName = string.Empty,
      };
    }

    private async Task<List<DelegationAuditEventServiceRoleGroupResponseInfo>> GetDelegationServiceRoleGroupLogs(DelegationAuditEvent auditEvent, List<OrganisationRole> allRoles)
    {
      var auditEventInfos = new List<DelegationAuditEventServiceRoleGroupResponseInfo>();

      if (auditEvent.EventType == DelegationAuditEventType.RoleAssigned.ToString() || auditEvent.EventType == DelegationAuditEventType.RoleUnassigned.ToString())
      {
        var roleIds = auditEvent.Roles.Split(",");
        var ccsRoleIds = allRoles.Where(r => roleIds.Any(x => x == Convert.ToString(r.RoleId))).Select(r => r.RoleId).ToList();
        var services = await _rolesToServiceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(ccsRoleIds);
        services = services.Distinct().ToList();

        foreach (var service in services)
        {
          var auditEventInfo = GetDelegationAuditEventServiceRoleGroupInfo(auditEvent);
          auditEventInfo.Name = service.Name;
          auditEventInfo.Key = service.Key;
          auditEventInfos.Add(auditEventInfo);
        }
      }
      else
      {
        var auditEventInfo = GetDelegationAuditEventServiceRoleGroupInfo(auditEvent);
        auditEventInfos.Add(auditEventInfo);
      }

      return auditEventInfos;
    }

    private static DelegationAuditEventServiceRoleGroupResponseInfo GetDelegationAuditEventServiceRoleGroupInfo(DelegationAuditEvent auditEvent)
    {
      return new DelegationAuditEventServiceRoleGroupResponseInfo
      {
        GroupId = auditEvent.GroupId,
        PreviousDelegationStartDate = auditEvent.PreviousDelegationStartDate,
        PreviousDelegationEndDate = auditEvent.PreviousDelegationEndDate,
        NewDelegationStartDate = auditEvent.NewDelegationStartDate,
        NewDelegationEndDate = auditEvent.NewDelegationEndDate,
        EventType = auditEvent.EventType,
        ActionedOnUtc = auditEvent.ActionedOnUtc,
        ActionedBy = auditEvent.ActionedBy,
        ActionedByUserName = auditEvent.ActionedByUserName,
        ActionedByFirstName = auditEvent.ActionedByFirstName,
        ActionedByLastName = auditEvent.ActionedByLastName,
        Name = string.Empty,
        Key = string.Empty,
      };
    }

  }
}
