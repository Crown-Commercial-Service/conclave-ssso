using CcsSso.Core.Domain.Dtos.External;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationSiteService
  {
    Task<int> CreateSiteAsync(string ciiOrganisationId, OrganisationSiteInfo organisationSiteInfo);

    Task DeleteSiteAsync(string ciiOrganisationId, int siteId);

    Task<OrganisationSiteInfoList> GetOrganisationSitesAsync(string ciiOrganisationId, string siteNameSerachString = null);

    Task<OrganisationSiteResponse> GetSiteAsync(string ciiOrganisationId, int siteId);

    Task UpdateSiteAsync(string ciiOrganisationId, int siteId, OrganisationSiteInfo organisationSiteInfo);
  }
}
