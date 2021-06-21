using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperOrganisationContactService
  {
    Task<WrapperOrganisationContactInfo> GetOrganisationContactPointAsync(string organisationId, int contactPointId);

    Task<WrapperOrganisationContactInfoList> GetOrganisationContactsAsync(string organisationId);

    Task<int> CreateOrganisationContactPointAsync(string organisationId, WrapperContactPointRequest wrapperContactPointRequest);

    Task UpdateOrganisationContactPointAsync(string organisationId, int contactPointId, WrapperContactPointRequest wrapperContactPointRequest);
  }
}
