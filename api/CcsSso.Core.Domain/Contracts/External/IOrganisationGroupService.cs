using CcsSso.Core.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationGroupService
  {
    Task<int> CreateGroupAsync(string ciiOrganisationId, OrganisationGroupNameInfo organisationGroupNameInfo);

    Task DeleteGroupAsync(string ciiOrganisationId, int groupId);

    Task<OrganisationGroupResponseInfo> GetGroupAsync(string ciiOrganisationId, int groupId);

    Task<OrganisationGroupList> GetGroupsAsync(string ciiOrganisationId, string searchString = null);

    Task UpdateGroupAsync(string ciiOrganisationId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo);

    Task<OrganisationServiceRoleGroupResponseInfo> GetServiceRoleGroupAsync(string ciiOrganisationId, int groupId);

    Task<GroupUserListResponse> GetGroupUsersPendingRequestSummary(int groupId, string ciiOrgId, ResultSetCriteria resultSetCriteria, bool isPendingApproval);


    Task UpdateServiceRoleGroupAsync(string ciiOrganisationId, int groupId, OrganisationServiceRoleGroupRequestInfo organisationServiceRoleGroupRequestInfo);

    Task<OrganisationGroupServiceRoleGroupList> GetGroupsServiceRoleGroupAsync(string ciiOrganisationId, string searchString = null);
  }
}
