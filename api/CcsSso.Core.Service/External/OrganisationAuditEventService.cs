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
  public partial class OrganisationAuditEventService : IOrganisationAuditEventService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dateTimeService;
    private readonly IServiceRoleGroupMapperService _rolesToServiceRoleGroupMapperService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;

    public OrganisationAuditEventService(IDataContext dataContext, IDateTimeService dateTimeService, IServiceRoleGroupMapperService rolesToServiceRoleGroupMapperService,
      ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _dataContext = dataContext;
      _dateTimeService = dateTimeService;
      _rolesToServiceRoleGroupMapperService = rolesToServiceRoleGroupMapperService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    /// <summary>
    /// Get organisation audit events
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="resultSetCriteria"></param>
    /// <returns></returns>
    public async Task<OrganisationAuditEventInfoListResponse> GetOrganisationAuditEventsListAsync(string ciiOrganisationId, ResultSetCriteria resultSetCriteria)
    {
      if (!ValidateCiiOrganisationID(ciiOrganisationId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      var auditLogs = await GetOrgAuditLogs(typeof(OrganisationAuditEventResponseInfo) ,ciiOrganisationId);

      int pageCount;
      var result = UtilityHelper.GetPagedResult(auditLogs, resultSetCriteria.CurrentPage, resultSetCriteria.PageSize, out pageCount);

      return new OrganisationAuditEventInfoListResponse
      {
        CurrentPage = resultSetCriteria.CurrentPage,
        PageCount = pageCount,
        RowCount = auditLogs.Count,
        OrganisationAuditEventList = result.Cast<OrganisationAuditEventResponseInfo>().ToList() ?? new List<OrganisationAuditEventResponseInfo>()
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

    private async Task<List<dynamic>> GetOrgAuditLogs(Type type, string ciiOrganisationId)
    {
      List<dynamic> auditEventInfos = new();

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
          if (type == typeof(OrganisationAuditEventResponseInfo))
          {
            auditEventInfos.AddRange(GetOrgRoleLogs(auditEvent, allRoles).ToList());
          }
          else if (type == typeof(OrgAuditEventServiceRoleGroupResponseInfo))
          {
            auditEventInfos.AddRange(await GetOrgServiceRoleGroupLogs(auditEvent, allRoles));
          }
      }

      auditEventInfos = auditEventInfos.OrderByDescending(x => x.Date).ToList();
      return auditEventInfos;
    }

    private static List<OrganisationAuditEventResponseInfo> GetOrgRoleLogs(OrganisationAuditEvent auditEvent, List<OrganisationRole> allRoles) 
    {
      var auditEventInfos = new List<OrganisationAuditEventResponseInfo>();

      if (auditEvent.Event == OrganisationAuditEventType.OrgRoleAssigned.ToString() || auditEvent.Event == OrganisationAuditEventType.OrgRoleUnassigned.ToString())
      {
        var roles = auditEvent.Roles.Split(",");

        foreach (var role in roles)
        {
          var roleInfo = allRoles.FirstOrDefault(x => x.RoleName?.Trim() == role?.Trim());
          if (roleInfo != null)
          {
            var auditEventInfo = new OrganisationAuditEventResponseInfo
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
            auditEventInfos.Add(auditEventInfo);
          }
        }
      }
      else if (auditEvent.Event != OrganisationAuditEventType.AdminRoleAssigned.ToString() && auditEvent.Event != OrganisationAuditEventType.AdminRoleUnassigned.ToString())
      {
        var auditEventInfo = new OrganisationAuditEventResponseInfo
        {
          OrganisationId = auditEvent.Organisation.CiiOrganisationId,
          FirstName = auditEvent.FirstName,
          LastName = auditEvent.LastName,
          GroupId = auditEvent.GroupId,
          Actioned = auditEvent.Actioned,
          ActionedBy = auditEvent.ActionedBy,
          Event = auditEvent.Event,
          Role = String.Empty,
          RoleKey = String.Empty,
          ServiceName = String.Empty,
          Date = auditEvent.Date
        };
        auditEventInfos.Add(auditEventInfo);
      }

      return auditEventInfos;
    }

    private void Validate(OrganisationAuditEventInfo organisationAuditEventInfo)
    {
      if (organisationAuditEventInfo.OrganisationId <= 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }
    }

    private static bool ValidateCiiOrganisationID(string CIIOrgID)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(CIIOrgID)) //OrgID mandatory
        {
          return false;
        }
        else if (CIIOrgID.Length != 18) // 18 Digits long
        {
          return false;
        }
        else if (CIIOrgID.StartsWith("0")) //No starting 0's
        {
          return false;
        }
        else if (!CIIOrgID.All(char.IsDigit)) //All characters are numbers 
        {
          return false;
        }
        return true;
      }
      catch (ArgumentException)
      {
      }
      return false;
    }

    #region ServiceRoleGroup
    public async Task<OrgAuditEventInfoServiceRoleGroupListResponse> GetOrganisationServiceRoleGroupAuditEventsListAsync(string ciiOrganisationId, ResultSetCriteria resultSetCriteria)
    {
      if (!_applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      if (!ValidateCiiOrganisationID(ciiOrganisationId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      var auditLogs = await GetOrgAuditLogs(typeof(OrgAuditEventServiceRoleGroupResponseInfo), ciiOrganisationId);

      int pageCount;
      var result = UtilityHelper.GetPagedResult(auditLogs, resultSetCriteria.CurrentPage, resultSetCriteria.PageSize, out pageCount);

      return new OrgAuditEventInfoServiceRoleGroupListResponse
      {
        CurrentPage = resultSetCriteria.CurrentPage,
        PageCount = pageCount,
        RowCount = auditLogs.Count,
        OrgAuditEventServiceRoleGroupList = result.Cast<OrgAuditEventServiceRoleGroupResponseInfo>().ToList() ?? new List<OrgAuditEventServiceRoleGroupResponseInfo>()
      };
    }

    private async Task<List<OrgAuditEventServiceRoleGroupResponseInfo>> GetOrgServiceRoleGroupLogs(OrganisationAuditEvent auditEvent, List<OrganisationRole> allRoles)
    {
      var auditEventInfos = new List<OrgAuditEventServiceRoleGroupResponseInfo>();

      if (auditEvent.Event == OrganisationAuditEventType.OrgRoleAssigned.ToString() || auditEvent.Event == OrganisationAuditEventType.OrgRoleUnassigned.ToString())
      {
        var roles = auditEvent.Roles.Split(",");
        var ccsRoleIds = allRoles.Where(r => roles.Any(x => x?.Trim() == r.RoleName?.Trim())).Select(r => r.RoleId).ToList();
        var services  = await _rolesToServiceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(ccsRoleIds);

        auditEventInfos = services.Distinct().Select(s => new OrgAuditEventServiceRoleGroupResponseInfo() 
        {
          OrganisationId = auditEvent.Organisation.CiiOrganisationId,
          FirstName = auditEvent.FirstName,
          LastName = auditEvent.LastName,
          GroupId = auditEvent.GroupId,
          Actioned = auditEvent.Actioned,
          ActionedBy = auditEvent.ActionedBy,
          Event = auditEvent.Event,
          Name = s.Name,
          Key = s.Key,
          Date = auditEvent.Date
        }).ToList();
        
      }
      else if (auditEvent.Event != OrganisationAuditEventType.AdminRoleAssigned.ToString() && auditEvent.Event != OrganisationAuditEventType.AdminRoleUnassigned.ToString())
      {
        var auditEventInfo = new OrgAuditEventServiceRoleGroupResponseInfo
        {
          OrganisationId = auditEvent.Organisation.CiiOrganisationId,
          FirstName = auditEvent.FirstName,
          LastName = auditEvent.LastName,
          GroupId = auditEvent.GroupId,
          Actioned = auditEvent.Actioned,
          ActionedBy = auditEvent.ActionedBy,
          Event = auditEvent.Event,
          Name = String.Empty,
          Key = String.Empty,
          Date = auditEvent.Date
        };
        auditEventInfos.Add(auditEventInfo);
      }

      return auditEventInfos;
    }
    #endregion
  }
}
