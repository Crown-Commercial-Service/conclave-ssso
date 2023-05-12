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

    public DelegationAuditEventService(IDataContext dataContext, IDateTimeService dateTimeService,
      IServiceRoleGroupMapperService rolesToServiceRoleGroupMapperService, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _dataContext = dataContext;
      _dateTimeService = dateTimeService;
      _rolesToServiceRoleGroupMapperService = rolesToServiceRoleGroupMapperService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
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
  }
}
