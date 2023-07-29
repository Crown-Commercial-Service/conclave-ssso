using System.Collections.Generic;
using System.Threading.Tasks;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Dtos.Domain.Models;


namespace CcsSso.Core.Domain.Contracts.Wrapper
{
    // #Auto validation
    public interface IWrapperOrganisationService
    {
        Task<WrapperOrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId);
        Task<OrganisationListResponseInfo> GetOrganisationDataAsync(OrganisationFilterCriteria organisationFilterCriteria, ResultSetCriteria resultSetCriteria);
    }
}
