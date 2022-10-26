﻿using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public partial class OrganisationAuditEventService : IOrganisationAuditEventService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dateTimeService;

    public OrganisationAuditEventService(IDataContext dataContext, IDateTimeService dateTimeService)
    {
      _dataContext = dataContext;
      _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// Get organisation audit events
    /// </summary>
    /// <param name="organisationId"></param>
    /// <returns></returns>
    public async Task<OrganisationAuditEventInfoList> GetOrganisationAuditEventsListAsync(int organisationId)
    {

      List<OrganisationAuditEventResponseInfo> auditEventInfos = new List<OrganisationAuditEventResponseInfo>();

      var auditEvents = await _dataContext.OrganisationAuditEvent
        .Where(c => c.OrganisationId == organisationId)
        .ToListAsync();

      foreach (var auditEvent in auditEvents)
      {
        var auditEventInfo = new OrganisationAuditEventResponseInfo
        {
          OrganisationId = auditEvent.OrganisationId,
          FirstName = auditEvent.FirstName,
          LastName = auditEvent.LastName,
          GroupId = auditEvent.GroupId,
          Actioned = auditEvent.Actioned,
          ActionedBy = auditEvent.ActionedBy,
          Event = auditEvent.Event,
          Roles = auditEvent.Roles
        };

        auditEventInfos.Add(auditEventInfo);
      }

      return new OrganisationAuditEventInfoList
      {
        Detail = new OrganisationAuditEventDetailInfo
        {
          OrganisationId = organisationId,
        },
        AuditEvents = auditEventInfos
      };
    }

    /// <summary>
    /// Create organisation audit events
    /// </summary>
    /// <param name="organisationAuditEventInfoList"></param>
    /// <returns></returns>
    public async Task CreateOrganisationAuditEventAsync(List<OrganisationAuditEventInfo> organisationAuditEventInfoList)
    {
      List<OrganisationAuditEvent> organisationAuditEvents = new List<OrganisationAuditEvent>();

      foreach (var organisationAuditEventInfo in organisationAuditEventInfoList)
      {
        Validate(organisationAuditEventInfo);

        var organisationAuditEvent = new OrganisationAuditEvent
        {
          OrganisationId = organisationAuditEventInfo.OrganisationId,
          SchemeIdentifier = organisationAuditEventInfo.SchemeIdentifier,
          FirstName = organisationAuditEventInfo.FirstName,
          LastName = organisationAuditEventInfo.LastName,
          GroupId = organisationAuditEventInfo.GroupId,
          Date = _dateTimeService.GetUTCNow(),
          Actioned = organisationAuditEventInfo.Actioned,
          ActionedBy = organisationAuditEventInfo.ActionedBy,
          Event = organisationAuditEventInfo.Event,
          Roles = organisationAuditEventInfo.Roles
        };

        organisationAuditEvents.Add(organisationAuditEvent);
      }

      _dataContext.OrganisationAuditEvent.AddRange(organisationAuditEvents);

      await _dataContext.SaveChangesAsync();
    }

    private void Validate(OrganisationAuditEventInfo organisationAuditEventInfo)
    {
      if (organisationAuditEventInfo.OrganisationId <= 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }
    }
  }
}
