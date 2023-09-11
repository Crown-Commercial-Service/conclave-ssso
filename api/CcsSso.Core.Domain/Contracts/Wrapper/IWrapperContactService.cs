using CcsSso.Domain.Dtos.External;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.Wrapper
{
  public interface IWrapperContactService
  {
    #region Organisation Contact
    Task<bool> DeleteOrganisationContactAsync(string organisationId, int contactId);
    Task<OrganisationContactInfoList> GetOrganisationContactListAsync(string organisationId);
    #endregion
  }
}
