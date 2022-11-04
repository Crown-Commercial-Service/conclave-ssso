using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public partial class OrganisationAuditService : IOrganisationAuditService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dateTimeService;

    public OrganisationAuditService(IDataContext dataContext, IDateTimeService dateTimeService)
    {
      _dataContext = dataContext;
      _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// Create organisation audit
    /// </summary>
    /// <param name="organisationAuditInfoList"></param>
    /// <returns></returns>
    public async Task CreateOrganisationAuditAsync(List<OrganisationAuditInfo> organisationAuditInfoList)
    {
      List<OrganisationAudit> organisationAudits = new();

      foreach (var organisationAuditInfo in organisationAuditInfoList)
      {
        Validate(organisationAuditInfo);

        var organisationAudit = new OrganisationAudit
        {
          OrganisationId = organisationAuditInfo.OrganisationId,
          SchemeIdentifier = organisationAuditInfo.SchemeIdentifier,
          Status = OrgAutoValidationStatus.AutoApproved,
          Actioned = organisationAuditInfo.Actioned,
          ActionedBy = organisationAuditInfo.ActionedBy,
          CreatedOnUtc = _dateTimeService.GetUTCNow(),
        };

        organisationAudits.Add(organisationAudit);
      }

      _dataContext.OrganisationAudit.AddRange(organisationAudits);

      await _dataContext.SaveChangesAsync();
    }

    /// <summary>
    /// Create organisation audit
    /// </summary>
    /// <param name="organisationAuditInfoList"></param>
    /// <returns></returns>
    public async Task CreateOrganisationAuditAsync(OrganisationAuditInfo organisationAuditInfo)
    {
      Validate(organisationAuditInfo);

      var organisationAudit = new OrganisationAudit
      {
        OrganisationId = organisationAuditInfo.OrganisationId,
        SchemeIdentifier = organisationAuditInfo.SchemeIdentifier,
        Status = organisationAuditInfo.Status,
        Actioned = organisationAuditInfo.Actioned,
        ActionedBy = organisationAuditInfo.ActionedBy,
        CreatedOnUtc = _dateTimeService.GetUTCNow(),
      };

      _dataContext.OrganisationAudit.Add(organisationAudit);

      await _dataContext.SaveChangesAsync();
    }

    private void Validate(OrganisationAuditInfo organisationAuditInfo)
    {
      if (organisationAuditInfo.OrganisationId <= 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }
    }
  }
}
