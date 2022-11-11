using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos.External;
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
    public async Task<OrganisationAuditEventInfoListResponse> GetOrganisationAuditEventsListAsync(int organisationId, ResultSetCriteria resultSetCriteria)
    {
      List<OrganisationAuditEventResponseInfo> auditEventInfos = new List<OrganisationAuditEventResponseInfo>();

      var auditEvents = await _dataContext.OrganisationAuditEvent
        .Where(c => c.OrganisationId == organisationId)
        .ToListAsync();

      foreach (var auditEvent in auditEvents)
      {
        if (auditEvent.Event == OrganisationAuditEventType.OrgRoleAssigned.ToString() || auditEvent.Event == OrganisationAuditEventType.OrgRoleUnassigned.ToString() ||
            auditEvent.Event == OrganisationAuditEventType.AdminRoleAssigned.ToString() || auditEvent.Event == OrganisationAuditEventType.AdminRoleUnassigned.ToString())
        {
          var roles = auditEvent.Roles.Split(",");

          foreach (var role in roles)
          {
            OrganisationAuditEventResponseInfo auditEventInfo = GetAuditEventInfo(auditEvent, role);
            auditEventInfos.Add(auditEventInfo);
          }
        }
        else
        {
          OrganisationAuditEventResponseInfo auditEventInfo = GetAuditEventInfo(auditEvent, "");
          auditEventInfos.Add(auditEventInfo);
        }
      }

      auditEventInfos = auditEventInfos.OrderByDescending(x => x.Date).ToList();

      int pageCount;
      var result = UtilityHelper.GetPagedResult(auditEventInfos, resultSetCriteria.CurrentPage, resultSetCriteria.PageSize, out pageCount);

      return new OrganisationAuditEventInfoListResponse
      {
        CurrentPage = resultSetCriteria.CurrentPage,
        PageCount = resultSetCriteria.PageSize,
        RowCount = auditEventInfos.Count,
        OrganisationAuditEventList = result ?? new List<OrganisationAuditEventResponseInfo>()
      };
    }

    private static OrganisationAuditEventResponseInfo GetAuditEventInfo(OrganisationAuditEvent auditEvent, string role)
    {
      return new OrganisationAuditEventResponseInfo
      {
        OrganisationId = auditEvent.OrganisationId,
        FirstName = auditEvent.FirstName,
        LastName = auditEvent.LastName,
        GroupId = auditEvent.GroupId,
        Actioned = auditEvent.Actioned,
        ActionedBy = auditEvent.ActionedBy,
        Event = auditEvent.Event,
        Role = role,
        Date = auditEvent.Date
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
