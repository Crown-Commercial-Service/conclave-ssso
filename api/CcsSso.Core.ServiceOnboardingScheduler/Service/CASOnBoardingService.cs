using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.ServiceOnboardingScheduler.Jobs;
using CcsSso.Core.ServiceOnboardingScheduler.Model;
using CcsSso.Core.ServiceOnboardingScheduler.Service.Contracts;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ServiceOnboardingScheduler.Service
{
  public class CASOnBoardingService : ICASOnBoardingService
  {
    private readonly IDataContext _dataContext;
    private readonly OnBoardingAppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;
    private readonly ILogger<CASOnBoardingService> _logger;

    public CASOnBoardingService(ILogger<CASOnBoardingService> logger, IServiceScopeFactory factory, OnBoardingAppSettings appSettings, IDateTimeService dataTimeService)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
    }

    public async Task<List<Organisation>> GetRegisteredOrgsIds(bool oneTimeValidationSwitch, DateTime startDate, DateTime endDate)
    {
      var dataDuration = _appSettings.OnBoardingDataDuration.CASOnboardingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        if (oneTimeValidationSwitch)
        {
          var organisationIds = await _dataContext.Organisation.Where(
                            org => !org.IsDeleted // && org.SupplierBuyerType > 0
                            && org.CreatedOnUtc >= TimeZoneInfo.ConvertTimeToUtc(startDate) && org.CreatedOnUtc <= TimeZoneInfo.ConvertTimeToUtc(endDate))
            .Select(o => new Organisation
            {
              Id = o.Id,
              CiiOrganisationId = o.CiiOrganisationId,
              LegalName = o.LegalName,
              CreatedOnUtc = o.CreatedOnUtc,
              RightToBuy = o.RightToBuy,
              SupplierBuyerType = o.SupplierBuyerType
            }).ToListAsync();

          return organisationIds;

        }
        else
        {
          var organisationIds = await _dataContext.Organisation.Where(
                  org => !org.IsDeleted && org.RightToBuy == false && org.SupplierBuyerType > 0
                  && org.CreatedOnUtc > untilDateTime)
                  .Select(o =>
                  new Organisation
                  {
                    Id = o.Id,
                    CiiOrganisationId = o.CiiOrganisationId,
                    LegalName = o.LegalName,
                    CreatedOnUtc = o.CreatedOnUtc,
                    RightToBuy = o.RightToBuy,
                    SupplierBuyerType = o.SupplierBuyerType
                  }).ToListAsync();

          return organisationIds;

        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }

    public async Task<List<Tuple<int, string, string, DateTime>>> GetOrgAdmins(int orgId, string ciiOrganisationId)
    {

      var orgAdminAccessRole = await _dataContext.OrganisationEligibleRole.FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == orgId && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey);

      if (orgAdminAccessRole != null)
      {
        var users = await _dataContext.User
                .Include(u => u.Party).ThenInclude(p => p.Person)
                .Include(u => u.UserAccessRoles)
                .Where(u => !u.IsDeleted &&
                u.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId &&  //u.AccountVerified == true &&
                u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRole.Id))
                .OrderBy(u => u.Party.Person.FirstName).ThenBy(u => u.Party.Person.LastName)
                .Select(o => new Tuple<int, string, string, DateTime>(o.Id, o.Party.Person.FirstName + o.Party.Person.FirstName, o.UserName, o.CreatedOnUtc)).ToListAsync();


        return users;
      }

      return null;

    }
  }
}
