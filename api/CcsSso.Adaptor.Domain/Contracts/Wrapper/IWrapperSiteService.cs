using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperSiteService
  {
    Task<WrapperOrganisationSiteInfoList> GetOrganisationSitesAsync(string organisationId);

    Task<WrapperOrganisationSiteResponse> GetOrganisationSiteAsync(string organisationId, int siteId);
  }
}
