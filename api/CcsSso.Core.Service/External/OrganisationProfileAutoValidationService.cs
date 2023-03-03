using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public partial class OrganisationProfileService : IOrganisationProfileService
  {

    public async Task<Tuple<bool, string>> AutoValidateOrganisationJob(string ciiOrganisationId)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }

      var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles)
                              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      //call lookup api
      return AutoValidateOrganisationDetails(ciiOrganisationId, "").Result;

    }

    public async Task<bool> AutoValidateOrganisationRoleFromJob(string ciiOrganisationId, AutoValidationOneTimeJobDetails autoValidationOneTimeJobDetails)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }

      var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles)
                              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      User actionedBy = await _dataContext.User.Include(p => p.Party).ThenInclude(pe => pe.Person).FirstOrDefaultAsync(x => !x.IsDeleted && x.UserName == autoValidationOneTimeJobDetails.AdminEmailId && x.UserType == UserType.Primary);

      // buyer and both only auto validated
      if ((organisation.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier))
      {
        // valid domain
        if (autoValidationOneTimeJobDetails.isDomainValid)
        {
          return await AutoValidateForValidDomain(organisation, actionedBy, autoValidationOneTimeJobDetails.CompanyHouseId, autoValidationOneTimeJobDetails.IsFromBackgroundJob);
        }
        // invalid domain
        else
        {
          return await AutoValidateForInValidDomain(organisation, actionedBy, autoValidationOneTimeJobDetails.CompanyHouseId, autoValidationOneTimeJobDetails.IsFromBackgroundJob);
        }
      }
      else
      {
        //Add supplier roles
        await SupplierRoleAssignmentAsync(organisation, autoValidationOneTimeJobDetails.AdminEmailId);
        return false;
      }

    }


  }
}
