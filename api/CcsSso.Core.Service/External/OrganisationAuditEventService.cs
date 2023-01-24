﻿using CcsSso.Core.DbModel.Constants;
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
    /// <param name="ciiOrganisationId"></param>
    /// <param name="resultSetCriteria"></param>
    /// <returns></returns>
    public async Task<OrganisationAuditEventInfoListResponse> GetOrganisationAuditEventsListAsync(string ciiOrganisationId, ResultSetCriteria resultSetCriteria)
    {
      List<OrganisationAuditEventResponseInfo> auditEventInfos = new List<OrganisationAuditEventResponseInfo>();

      var auditEvents = await _dataContext.OrganisationAuditEvent
        .Include(x => x.Organisation)
        .Where(c => c.Organisation.CiiOrganisationId == ciiOrganisationId)
        .ToListAsync();

      var allRoles = await _dataContext.CcsAccessRole.Where(r => !r.IsDeleted)
                          .Include(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                          .Select(i => new OrganisationRole
                          {
                            RoleId = i.Id,
                            RoleName = i.CcsAccessRoleName,
                            RoleKey = i.CcsAccessRoleNameKey,
                            ServiceName = i.ServiceRolePermissions.FirstOrDefault().ServicePermission.CcsService.ServiceName,
                          }).ToListAsync();

      foreach (var auditEvent in auditEvents)
      {
        if (auditEvent.Event == OrganisationAuditEventType.OrgRoleAssigned.ToString() || auditEvent.Event == OrganisationAuditEventType.OrgRoleUnassigned.ToString())
        {
          var roles = auditEvent.Roles.Split(",");

          foreach (var role in roles)
          {
            var roleKey = allRoles.FirstOrDefault(x => x.RoleName == role?.Trim());
            OrganisationAuditEventResponseInfo auditEventInfo = GetAuditEventInfo(auditEvent, role, roleKey);
            auditEventInfos.Add(auditEventInfo);
          }
        }
        else if(auditEvent.Event != OrganisationAuditEventType.AdminRoleAssigned.ToString() && auditEvent.Event != OrganisationAuditEventType.AdminRoleUnassigned.ToString())
        {
          OrganisationAuditEventResponseInfo auditEventInfo = GetAuditEventInfo(auditEvent, "", null);
          auditEventInfos.Add(auditEventInfo);
        }
      }

      auditEventInfos = auditEventInfos.OrderByDescending(x => x.Date).ToList();

      int pageCount;
      var result = UtilityHelper.GetPagedResult(auditEventInfos, resultSetCriteria.CurrentPage, resultSetCriteria.PageSize, out pageCount);

      return new OrganisationAuditEventInfoListResponse
      {
        CurrentPage = resultSetCriteria.CurrentPage,
        PageCount = pageCount,
        RowCount = auditEventInfos.Count,
        OrganisationAuditEventList = result ?? new List<OrganisationAuditEventResponseInfo>()
      };
    }

    private static OrganisationAuditEventResponseInfo GetAuditEventInfo(OrganisationAuditEvent auditEvent, string role, OrganisationRole roleInfo)
    {
      return new OrganisationAuditEventResponseInfo
      {
        OrganisationId = auditEvent.Organisation.CiiOrganisationId,
        FirstName = auditEvent.FirstName,
        LastName = auditEvent.LastName,
        GroupId = auditEvent.GroupId,
        Actioned = auditEvent.Actioned,
        ActionedBy = auditEvent.ActionedBy,
        Event = auditEvent.Event,
        Role = role,
        RoleKey = roleInfo?.RoleKey,
        ServiceName = roleInfo?.ServiceName,
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
