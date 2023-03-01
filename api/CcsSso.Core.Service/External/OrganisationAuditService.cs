using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
    /// To get list of organisation audits with pagination
    /// </summary>
    /// <param name="resultSetCriteria"></param>
    /// <param name="organisationAuditFilterCriteria"></param>
    /// <returns></returns>
    public async Task<OrganisationAuditInfoListResponse> GetAllAsync(ResultSetCriteria resultSetCriteria, OrganisationAuditFilterCriteria organisationAuditFilterCriteria)
    {
      var organisations = await _dataContext.GetPagedResultAsync(_dataContext.OrganisationAudit
        .Include(o => o.Organisation)
        .Where(x => (string.IsNullOrEmpty(organisationAuditFilterCriteria.searchString) || x.Organisation.LegalName.ToLower().Contains(organisationAuditFilterCriteria.searchString.ToLower()))
        && (organisationAuditFilterCriteria.isPendingOnly || (x.Status != OrgAutoValidationStatus.AutoPending && x.Status != OrgAutoValidationStatus.ManualPending))
        && (!organisationAuditFilterCriteria.isPendingOnly || x.Status == OrgAutoValidationStatus.AutoPending || x.Status == OrgAutoValidationStatus.ManualPending))
        .OrderByDescending(x => x.Organisation.CreatedOnUtc)
        .Select(organisationAudit => new OrganisationAuditResponseInfo
        {
          OrganisationId = organisationAudit.Organisation.CiiOrganisationId,
          OrganisationName = organisationAudit.Organisation.LegalName,
          OrganisationType = organisationAudit.Organisation.SupplierBuyerType != null ? (int)organisationAudit.Organisation.SupplierBuyerType : 0,
          DateOfRegistration = organisationAudit.Organisation.CreatedOnUtc,
          RightToBuy = organisationAudit.Organisation.RightToBuy,
          AuditStatus = organisationAudit.Status
        }), resultSetCriteria);

      var orgListResponse = new OrganisationAuditInfoListResponse
      {
        CurrentPage = organisations.CurrentPage,
        PageCount = organisations.PageCount,
        RowCount = organisations.RowCount,
        OrganisationAuditList = organisations.Results ?? new List<OrganisationAuditResponseInfo>()
      };

      return orgListResponse;
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
          ActionedOnUtc = _dateTimeService.GetUTCNow(),
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
        ActionedOnUtc = _dateTimeService.GetUTCNow(),
      };

      _dataContext.OrganisationAudit.Add(organisationAudit);

      await _dataContext.SaveChangesAsync();
    }

    /// <summary>
    /// To update organisation audit
    /// </summary>
    /// <param name="organisationAuditInfo"></param>
    /// <returns></returns>
    public async Task UpdateOrganisationAuditAsync(OrganisationAuditInfo organisationAuditInfo)
    {
      Validate(organisationAuditInfo);

      var organisationAudit = _dataContext.OrganisationAudit.FirstOrDefault(x => x.OrganisationId == organisationAuditInfo.OrganisationId);
      if (organisationAudit != null)
      {
        organisationAudit.Status = organisationAuditInfo.Status;
        organisationAudit.Actioned = organisationAuditInfo.Actioned;
        organisationAudit.ActionedBy = organisationAuditInfo.ActionedBy;
        organisationAudit.ActionedOnUtc = _dateTimeService.GetUTCNow();

        await _dataContext.SaveChangesAsync();
      }
      else {
        await CreateOrganisationAuditAsync(organisationAuditInfo);
      }
    }

    private void Validate(OrganisationAuditInfo organisationAuditInfo)
    {
      if (organisationAuditInfo == null || organisationAuditInfo.OrganisationId <= 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }
    }
  }
}
