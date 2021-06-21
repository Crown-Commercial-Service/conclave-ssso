using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperOrganisationService
  {
    Task<WrapperOrganisationResponse> GetOrganisationAsync(string organisationId);

    Task<List<WrapperUserListInfo>> GetOrganisationUsersAsync(string organisationId);

    Task<string> UpdateOrganisationAsync(string organisationId, WrapperOrganisationRequest wrapperOrganisationRequest);
  }
}
