using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperSiteContactService
  {
    Task<WrapperOrganisationSiteContactInfo> GetSiteContactPointAsync(string organisationId, int siteId, int contactPointId);

    Task<WrapperOrganisationSiteContactInfoList> GetSiteContactPointsAsync(string organisationId, int siteId);

    Task<int> CreateSiteContactPointAsync(string organisationId, int siteId, WrapperContactPointRequest wrapperContactPointRequest);

    Task UpdateSiteContactPointAsync(string organisationId, int siteId, int contactPointId, WrapperContactPointRequest wrapperContactPointRequest);
  }
}
