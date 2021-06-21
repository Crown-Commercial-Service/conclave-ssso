using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperContactService
  {
    Task<WrapperContactResponse> GetContactAsync(int contactId);

    Task<int> CreateContactAsync(WrapperContactRequest wrapperContactRequest);

    Task UpdateContactAsync(int contactId, WrapperContactRequest wrapperContactRequest);
  }
}
