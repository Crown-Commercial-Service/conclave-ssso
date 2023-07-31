using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.Wrapper
{
    public interface IWrapperConfigurationService
    {
        Task<List<RoleApprovalConfigurationInfo>> GetRoleApprovalConfigurationsAsync();
        Task<List<ServiceRoleGroup>> GetServiceRoleGroupsRequireApproval();
    }
}
