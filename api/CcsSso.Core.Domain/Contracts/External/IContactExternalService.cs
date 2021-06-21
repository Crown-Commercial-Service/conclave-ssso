using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IContactExternalService
  {
    Task<int> CreateAsync(ContactRequestDetail contactRequestDetail);

    Task DeleteAsync(int id);

    Task<ContactResponseDetail> GetAsync(int id);

    Task UpdateAsync(int id, ContactRequestDetail contactRequestDetail);
  }
}
