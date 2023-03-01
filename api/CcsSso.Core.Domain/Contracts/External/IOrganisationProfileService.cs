using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationProfileService
  {
    Task<string> CreateOrganisationAsync(OrganisationProfileInfo organisationProfileInfo);

    // Deletion has commented out since its not going to be exposed via the api at the moment
    // Task DeleteOrganisationAsync(string ciiOrganisationId);

    Task<OrganisationProfileResponseInfo> GetOrganisationAsync(string ciiOrganisationId);

    Task<List<IdentityProviderDetail>> GetOrganisationIdentityProvidersAsync(string ciiOrganisationId);

    Task<List<OrganisationRole>> GetOrganisationRolesAsync(string ciiOrganisationId);

    Task UpdateOrganisationAsync(string ciiOrganisationId, OrganisationProfileInfo organisationProfileInfo);

    Task UpdateIdentityProviderAsync(OrgIdentityProviderSummary orgIdentityProviderSummary);

    Task UpdateOrganisationEligibleRolesAsync(string ciiOrganisationId, bool isBuyer, List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete);

    // #Auto validation
    Task<Tuple<bool, string>> AutoValidateOrganisationJob(string ciiOrganisationId);
    Task<bool> AutoValidateOrganisationRoleFromJob(string ciiOrganisationId,  AutoValidationOneTimeJobDetails autoValidationOneTimeJobDetails);

    Task<bool> AutoValidateOrganisationRegistration(string organisationId, AutoValidationDetails autoValidationDetails);

    Task UpdateOrgAutoValidationEligibleRolesAsync(string ciiOrganisationId, RoleEligibleTradeType orgType, List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete, List<OrganisationRole> rolesToAutoValid, string? companyHouseId);

    Task<Tuple<bool, string>> AutoValidateOrganisationDetails(string ciiOrganisationId, string adminEmailId = "");
    Task ManualValidateOrganisation(string ciiOrganisationId, ManualValidateOrganisationStatus status);

    Task<List<ServiceRoleGroup>> GetOrganisationServiceRoleGroupsAsync(string ciiOrganisationId);

    Task UpdateOrganisationEligibleServiceRoleGroupsAsync(string ciiOrganisationId, bool isBuyer, List<int> serviceRoleGroupsToAdd, List<int> serviceRoleGroupsToDelete);

    Task UpdateOrgAutoValidServiceRoleGroupsAsync(string ciiOrganisationId, RoleEligibleTradeType orgType, List<int> serviceRoleGroupsToAdd, List<int> serviceRoleGroupsToDelete, List<int> serviceRoleGroupsToAutoValid, string? companyHouseId);

  }
}
